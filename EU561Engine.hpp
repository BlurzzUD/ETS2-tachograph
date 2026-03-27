#pragma once
#include <cstdint>
#include <vector>
#include <string>
#include <chrono>

struct Fine {
    std::string rule;
    float       amountEUR;
    std::string severity;
};

class EU561Engine {
public:

    static constexpr float CONTINUOUS_DRIVE_MAX  = 4.5f * 3600.f;
    static constexpr float BREAK_MIN             = 45.f  * 60.f;
    static constexpr float BREAK_SPLIT_FIRST     = 15.f  * 60.f;
    static constexpr float BREAK_SPLIT_SECOND    = 30.f  * 60.f;
    static constexpr float DAILY_DRIVE_NORMAL    = 9.f   * 3600.f;
    static constexpr float DAILY_DRIVE_EXTENDED  = 10.f  * 3600.f;
    static constexpr float WEEKLY_DRIVE_MAX      = 56.f  * 3600.f;
    static constexpr float FORTNIGHTLY_MAX       = 90.f  * 3600.f;
    static constexpr float DAILY_REST_MIN        = 11.f  * 3600.f;
    static constexpr float DAILY_REST_REDUCED    = 9.f   * 3600.f;
    static constexpr float WEEKLY_REST_MIN       = 45.f  * 3600.f;
    static constexpr float WEEKLY_REST_REDUCED   = 24.f  * 3600.f;

    EU561Engine() { Reset(); }

    void Reset() {
        continuousDrive    = 0.f;
        dailyDrive         = 0.f;
        weeklyDrive        = 0.f;
        fortnightlyDrive   = 0.f;
        currentRest        = 0.f;
        extendedDaysUsed   = 0;
        reducedRestUsed    = 0;
        lastDriving        = false;
        violations.clear();
        totalFines         = 0.f;
    }

    void Tick(bool isDriving, float dtSec) {
        if (isDriving) {
            continuousDrive  += dtSec;
            dailyDrive       += dtSec;
            weeklyDrive      += dtSec;
            fortnightlyDrive += dtSec;

            if (!lastDriving) currentRest = 0.f;

            if (continuousDrive > CONTINUOUS_DRIVE_MAX + 60.f) {
                AddViolation("Continuous drive > 4h30m", Severity::Serious);
            }

            float dailyLimit = (extendedDaysUsed < 2)
                               ? DAILY_DRIVE_EXTENDED
                               : DAILY_DRIVE_NORMAL;
            if (dailyDrive > dailyLimit + 60.f) {
                AddViolation("Daily drive limit exceeded", Severity::VerySerious);
            }

            if (weeklyDrive > WEEKLY_DRIVE_MAX + 60.f) {
                AddViolation("Weekly 56h limit exceeded", Severity::VerySerious);
            }

        } else {

            currentRest += dtSec;

            if (currentRest >= BREAK_MIN) {
                continuousDrive = 0.f;
            }

            if (currentRest >= DAILY_REST_MIN) {
                float saved = dailyDrive;
                dailyDrive = 0.f;
                if (saved >= DAILY_DRIVE_EXTENDED - 60.f) extendedDaysUsed++;

                if (currentRest >= WEEKLY_REST_MIN) {
                    weeklyDrive      = 0.f;
                    fortnightlyDrive = 0.f;
                    extendedDaysUsed = 0;
                    reducedRestUsed  = 0;
                }
            }
        }
        lastDriving = isDriving;
    }

    float BreakDueIn()      const { return CONTINUOUS_DRIVE_MAX - continuousDrive; }

    float DailyRemaining()  const {
        float limit = (extendedDaysUsed < 2) ? DAILY_DRIVE_EXTENDED : DAILY_DRIVE_NORMAL;
        return limit - dailyDrive;
    }

    float WeeklyRemaining() const { return WEEKLY_DRIVE_MAX - weeklyDrive; }

    float CurrentRest()     const { return currentRest; }

    float ContinuousDrive() const { return continuousDrive; }
    float DailyDrive()      const { return dailyDrive; }
    float WeeklyDrive()     const { return weeklyDrive; }

    float BreakStillNeeded() const {
        float remaining = BREAK_MIN - currentRest;
        return remaining > 0 ? remaining : 0.f;
    }

    bool IsBreakRequired()  const { return continuousDrive >= CONTINUOUS_DRIVE_MAX; }
    bool IsLegal()          const { return violations.empty(); }

    const std::vector<Fine>& Violations() const { return violations; }
    float TotalFines()      const { return totalFines; }

    static std::string FormatTime(float seconds) {
        int s = (int)fabsf(seconds);
        bool neg = seconds < 0;
        char buf[32];
        snprintf(buf, sizeof(buf), "%s%02d:%02d:%02d",
                 neg ? "-" : "",
                 s / 3600, (s % 3600) / 60, s % 60);
        return buf;
    }

private:
    float continuousDrive;
    float dailyDrive;
    float weeklyDrive;
    float fortnightlyDrive;
    float currentRest;
    float totalFines;
    int   extendedDaysUsed;
    int   reducedRestUsed;
    bool  lastDriving;

    std::vector<Fine> violations;

    enum class Severity { Minor, Serious, VerySerious };

    void AddViolation(const char* rule, Severity sev) {

        for (auto& v : violations)
            if (v.rule == rule) return;

        Fine f;
        f.rule = rule;
        switch (sev) {
        case Severity::Minor:
            f.severity = "Minor";
            f.amountEUR = 100.f;
            break;
        case Severity::Serious:
            f.severity = "Serious";
            f.amountEUR = 300.f;
            break;
        case Severity::VerySerious:
            f.severity = "Very Serious";
            f.amountEUR = 800.f;
            break;
        }
        violations.push_back(f);
        totalFines += f.amountEUR;
    }
};
