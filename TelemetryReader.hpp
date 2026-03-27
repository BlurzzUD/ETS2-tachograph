#pragma once
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdint>
#include <cmath>
#include <string>

#pragma pack(push, 1)

struct SCSTelemetryMap {
     uint32_t   time;
     uint32_t   paused;
     uint32_t   pluginRevision;
     uint32_t   ets2VersionMajor;
     uint32_t   ets2VersionMinor;
     uint8_t    flags[4];
     float      speed;
     float      accelerationX;
     float      accelerationY;
     float      accelerationZ;
     float      coordinateX;
     float      coordinateY;
     float      coordinateZ;
     float      rotationX;
     float      rotationY;
     float      rotationZ;
     int32_t    gear;
     int32_t    gears;
     int32_t    gearRanges;
     int32_t    gearRangeActive;
     float      engineRpm;
     float      engineRpmMax;
     float      fuel;
     float      fuelCapacity;
     float      fuelRate;
     float      fuelAvgConsumption;
     float      userSteer;
     float      userThrottle;
     float      userBrake;
     float      userClutch;
     float      gameSteer;
     float      gameThrottle;
     float      gameBrake;
     float      gameClutch;
     float      truckWeight;
     float      trailerWeight;
     int32_t    modelOffset;
     int32_t    modelLength;
     int32_t    trailerOffset;
     int32_t    trailerLength;
     int32_t    timeAbsolute;
     int32_t    gearsReverse;
     float      trailerMass;
     uint8_t    trailerId[64];
     uint8_t    trailerName[64];
     int32_t    jobIncome;
     int32_t    jobDeadline;
     uint8_t    jobCitySource[64];
     uint8_t    jobCityDestination[64];
     uint8_t    jobCompanySource[64];
     uint8_t    jobCompanyDestination[64];
     int32_t    retarderBrake;
     int32_t    shifterSlot;
     int32_t    shifterToggle;
     uint8_t    _pad[4];
     uint8_t    aux[24];
     float      airPressure;
     float      brakeTemperature;
     int32_t    fuelWarning;
     float      adblue;
     float      adblueConsumption;
     float      oilPressure;
     float      oilTemperature;
     float      waterTemperature;
     float      batteryVoltage;
     float      lightsDashboard;
     float      wearEngine;
     float      wearTransmission;
     float      wearCabin;
     float      wearChassis;
     float      wearWheels;
     float      wearTrailer;
     float      truckOdometer;
     float      cruiseControlSpeed;
     uint8_t    truckMake[64];
     uint8_t    truckMakeId[64];
     uint8_t    truckModel[64];
     float      speedLimit;
     float      routeDistance;
     float      routeTime;
     float      fuelRange;
     float      gearRatioForward[24];
     float      gearRatioReverse[8];
     float      gearRatioDifferential;
     int32_t    gearDashboard;
     uint8_t    onJob;
     uint8_t    jobFinished;
};

#pragma pack(pop)

class TelemetryReader {
public:
    TelemetryReader() : hMap(nullptr), pData(nullptr) {}
    ~TelemetryReader() { Close(); }

    bool Open() {
        hMap = OpenFileMappingA(FILE_MAP_READ, FALSE, "Local\\SimTelemetryETS2");
        if (!hMap)
            hMap = OpenFileMappingA(FILE_MAP_READ, FALSE, "Local\\SCSTelemetry");
        if (!hMap) return false;
        pData = static_cast<SCSTelemetryMap*>(
            MapViewOfFile(hMap, FILE_MAP_READ, 0, 0, sizeof(SCSTelemetryMap))
        );
        return pData != nullptr;
    }

    void Close() {
        if (pData) { UnmapViewOfFile(pData); pData = nullptr; }
        if (hMap)  { CloseHandle(hMap);      hMap  = nullptr; }
    }

    bool IsConnected()  const { return pData != nullptr; }
    bool IsPaused()     const { return pData && pData->paused; }
    float SpeedKmh()    const { return pData ? fabsf(pData->speed) * 3.6f : 0.f; }
    float FuelLitres()  const { return pData ? pData->fuel : 0.f; }
    float FuelCapacity()const { return pData ? pData->fuelCapacity : 1.f; }
    float FuelPercent() const {
        if (!pData || pData->fuelCapacity <= 0.f) return 0.f;
        return (pData->fuel / pData->fuelCapacity) * 100.f;
    }
    float Odometer()    const { return pData ? pData->truckOdometer : 0.f; }
    int   Gear()        const { return pData ? pData->gear : 0; }
    int   GearDash()    const { return pData ? pData->gearDashboard : 0; }
    float RPM()         const { return pData ? pData->engineRpm : 0.f; }
    float SpeedLimit()  const { return pData ? pData->speedLimit * 3.6f : 0.f; }
    float RouteDistKm() const { return pData ? pData->routeDistance / 1000.f : 0.f; }
    float RouteSec()    const { return pData ? pData->routeTime : 0.f; }
    bool  TrailerAttached() const { return pData && pData->flags[1] > 0; }
    bool  OnJob()       const { return pData && pData->onJob > 0; }
    bool  CruiseControl()     const { return pData && pData->aux[0]  > 0; }
    bool  ParkBrake()         const { return pData && pData->aux[2]  > 0; }
    bool  MotorBrake()        const { return pData && pData->aux[3]  > 0; }
    bool  EngineOn()          const { return pData && pData->aux[5]  > 0; }
    bool  BlinkerLeftActive() const { return pData && pData->aux[6]  > 0; }
    bool  BlinkerRightActive()const { return pData && pData->aux[7]  > 0; }
    bool  LightsLow()         const { return pData && pData->aux[11] > 0; }
    bool  LightsHigh()        const { return pData && pData->aux[12] > 0; }
    bool  IsDriving()   const { return !IsPaused() && SpeedKmh() > 1.0f; }
    std::string CitySource()      const { return pData ? (char*)pData->jobCitySource      : ""; }
    std::string CityDestination() const { return pData ? (char*)pData->jobCityDestination : ""; }
    std::string TruckMake()       const { return pData ? (char*)pData->truckMake          : ""; }
    std::string TruckModel()      const { return pData ? (char*)pData->truckModel         : ""; }
    int   GameTimeMin() const { return pData ? pData->timeAbsolute : 0; }
    const SCSTelemetryMap* Raw()  const { return pData; }

private:
    HANDLE           hMap;
    SCSTelemetryMap* pData;
};