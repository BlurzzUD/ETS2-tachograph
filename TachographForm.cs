using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Ets2Tacho
{
    public class Ets2SdkData
    {
        public uint  paused;
        public float speed;
        public int   gear;
        public float engineRpm;
        public float engineRpmMax;
        public float fuel;
        public float fuelCapacity;
        public int   timeAbsolute;
        public float truckOdometer;
        public float speedLimit;
        public float routeDistance;
        public int   gearDashboard;
        public byte  onJob;

        public byte auxCruiseCtrl;
        public byte auxParkBrake;
        public byte auxEngineOn;
        public byte auxLightsLow;

        public string jobCitySource      = "";
        public string jobCityDestination = "";
        public string truckMake          = "";

        public float SpeedKmh       => Math.Abs(speed) * 3.6f;
        public float SpeedLimitKmh  => speedLimit * 3.6f;
        public bool  EngineOn       => auxEngineOn   > 0;
        public bool  ParkBrake      => auxParkBrake  > 0;
        public bool  CruiseCtrl     => auxCruiseCtrl > 0;
        public bool  LightsLow      => auxLightsLow  > 0;
        public string CityFrom      => jobCitySource;
        public string CityTo        => jobCityDestination;
        public string Truck         => truckMake;

        public static Ets2SdkData FromPtr(IntPtr p)
        {
            var d = new Ets2SdkData();
            d.paused          = (uint)Marshal.ReadInt32(p, 4);
            d.speed           = ReadFloat(p, 24);
            d.gear            = Marshal.ReadInt32(p, 64);
            d.engineRpm       = ReadFloat(p, 80);
            d.engineRpmMax    = ReadFloat(p, 84);
            d.fuel            = ReadFloat(p, 88);
            d.fuelCapacity    = ReadFloat(p, 92);
            d.timeAbsolute    = Marshal.ReadInt32(p, 160);
            d.truckOdometer   = ReadFloat(p, 668);
            d.speedLimit      = ReadFloat(p, 868);
            d.routeDistance   = ReadFloat(p, 872);
            d.gearDashboard   = Marshal.ReadInt32(p, 1016);
            d.onJob           = Marshal.ReadByte(p, 1020);
            d.auxCruiseCtrl   = Marshal.ReadByte(p, 580 + 0);
            d.auxParkBrake    = Marshal.ReadByte(p, 580 + 2);
            d.auxEngineOn     = Marshal.ReadByte(p, 580 + 5);
            d.auxLightsLow    = Marshal.ReadByte(p, 580 + 11);
            d.jobCitySource      = ReadString(p, 308, 64);
            d.jobCityDestination = ReadString(p, 372, 64);
            d.truckMake          = ReadString(p, 676, 64);
            return d;
        }

        static float ReadFloat(IntPtr p, int offset)
        {
            int raw = Marshal.ReadInt32(p, offset);
            byte[] bytes = BitConverter.GetBytes(raw);
            return BitConverter.ToSingle(bytes, 0);
        }

        static string ReadString(IntPtr p, int offset, int maxLen)
        {
            byte[] buf = new byte[maxLen];
            for (int i = 0; i < maxLen; i++)
                buf[i] = Marshal.ReadByte(p, offset + i);
            int len = Array.IndexOf(buf, (byte)0);
            if (len < 0) len = maxLen;
            return Encoding.UTF8.GetString(buf, 0, len);
        }
    }

    public class EU561Engine
    {
        public const float CONTINUOUS_MAX = 4.5f * 3600f;
        public const float BREAK_MIN      = 45f  * 60f;
        public const float DAILY_NORMAL   = 9f   * 3600f;
        public const float DAILY_EXTENDED = 10f  * 3600f;
        public const float WEEKLY_MAX     = 56f  * 3600f;

        public float ContinuousDrive { get; set; }
        public float DailyDrive      { get; set; }
        public float WeeklyDrive     { get; set; }
        public float CurrentRest     { get; set; }
        public bool  IsViolation     { get; set; }
        public float TotalFines      { get; set; }
        public string LastViolation  { get; set; } = "";
        public int   ExtendedDaysUsed { get; set; }

        private bool lastDriving;

        public float BreakDueIn      => CONTINUOUS_MAX - ContinuousDrive;
        public float DailyRemaining  => (ExtendedDaysUsed < 2 ? DAILY_EXTENDED : DAILY_NORMAL) - DailyDrive;
        public float WeeklyRemaining => WEEKLY_MAX - WeeklyDrive;
        public bool  BreakRequired   => ContinuousDrive >= CONTINUOUS_MAX;

        public void Tick(bool driving, float dt)
        {
            if (driving)
            {
                ContinuousDrive += dt;
                DailyDrive      += dt;
                WeeklyDrive     += dt;
                if (!lastDriving) CurrentRest = 0f;

                if (ContinuousDrive > CONTINUOUS_MAX + 60f && !IsViolation)
                {
                    IsViolation   = true;
                    TotalFines   += 300f;
                    LastViolation = "Continuous drive >4h30m  EUR 300";
                }
                if (DailyDrive > DAILY_EXTENDED + 60f && !IsViolation)
                {
                    IsViolation   = true;
                    TotalFines   += 800f;
                    LastViolation = "Daily limit exceeded  EUR 800";
                }
            }
            else
            {
                CurrentRest += dt;
                if (CurrentRest >= BREAK_MIN)   ContinuousDrive = 0f;
                if (CurrentRest >= 11f * 3600f) { DailyDrive = 0f; IsViolation = false; }
                if (CurrentRest >= 45f * 3600f) { WeeklyDrive = 0f; ExtendedDaysUsed = 0; }
            }
            lastDriving = driving;
        }

        public void Reset()
        {
            ContinuousDrive = DailyDrive = WeeklyDrive = CurrentRest = TotalFines = 0f;
            IsViolation = false; LastViolation = ""; ExtendedDaysUsed = 0; lastDriving = false;
        }

        public static string Fmt(float s)
        {
            bool neg = s < 0; int a = (int)Math.Abs(s);
            return $"{(neg?"-":"")}{a/3600:00}:{(a%3600)/60:00}:{a%60:00}";
        }
    }

    public class TachoCache
    {
        static readonly string CachePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "tacho_cache.dat");

        public static void Save(EU561Engine engine)
        {
            try
            {
                var lines = new[]
                {
                    engine.ContinuousDrive.ToString("F2"),
                    engine.DailyDrive.ToString("F2"),
                    engine.WeeklyDrive.ToString("F2"),
                    engine.CurrentRest.ToString("F2"),
                    engine.TotalFines.ToString("F2"),
                    engine.IsViolation ? "1" : "0",
                    engine.LastViolation,
                    engine.ExtendedDaysUsed.ToString(),
                    DateTime.UtcNow.ToBinary().ToString()
                };
                File.WriteAllLines(CachePath, lines);
            }
            catch { }
        }

        public static void Load(EU561Engine engine)
        {
            try
            {
                if (!File.Exists(CachePath)) return;
                var lines = File.ReadAllLines(CachePath);
                if (lines.Length < 9) return;

                long savedBinary = long.Parse(lines[8]);
                DateTime savedTime = DateTime.FromBinary(savedBinary);
                double minutesElapsed = (DateTime.UtcNow - savedTime).TotalMinutes;

                engine.ContinuousDrive  = float.Parse(lines[0]);
                engine.DailyDrive       = float.Parse(lines[1]);
                engine.WeeklyDrive      = float.Parse(lines[2]);
                engine.CurrentRest      = float.Parse(lines[3]);
                engine.TotalFines       = float.Parse(lines[4]);
                engine.IsViolation      = lines[5] == "1";
                engine.LastViolation    = lines[6];
                engine.ExtendedDaysUsed = int.Parse(lines[7]);

                if (minutesElapsed > 0)
                {
                    float elapsedSec = (float)(minutesElapsed * 60.0);
                    engine.CurrentRest += elapsedSec;
                    if (engine.CurrentRest >= EU561Engine.BREAK_MIN)
                        engine.ContinuousDrive = 0f;
                    if (engine.CurrentRest >= 11f * 3600f)
                    {
                        engine.DailyDrive  = 0f;
                        engine.IsViolation = false;
                    }
                    if (engine.CurrentRest >= 45f * 3600f)
                    {
                        engine.WeeklyDrive      = 0f;
                        engine.ExtendedDaysUsed = 0;
                    }
                }
            }
            catch { }
        }
    }

    public class TelemetryReader : IDisposable
    {
        [DllImport("kernel32.dll")] static extern IntPtr OpenFileMapping(uint access, bool inherit, string name);
        [DllImport("kernel32.dll")] static extern IntPtr MapViewOfFile(IntPtr h, uint access, uint hi, uint lo, uint size);
        [DllImport("kernel32.dll")] static extern bool UnmapViewOfFile(IntPtr addr);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);

        const uint FILE_MAP_READ = 4;
        const uint MAP_SIZE      = 1200;
        IntPtr hMap = IntPtr.Zero, pView = IntPtr.Zero;

        public bool Connected => pView != IntPtr.Zero;

        public TelemetryReader()
        {
            hMap = OpenFileMapping(FILE_MAP_READ, false, "Local\\SimTelemetryETS2");
            if (hMap == IntPtr.Zero)
                hMap = OpenFileMapping(FILE_MAP_READ, false, "Local\\SCSTelemetry");
            if (hMap != IntPtr.Zero)
                pView = MapViewOfFile(hMap, FILE_MAP_READ, 0, 0, MAP_SIZE);
        }

        public Ets2SdkData Read()
        {
            if (!Connected) return new Ets2SdkData();
            return Ets2SdkData.FromPtr(pView);
        }

        public void Dispose()
        {
            if (pView != IntPtr.Zero) { UnmapViewOfFile(pView); pView = IntPtr.Zero; }
            if (hMap  != IntPtr.Zero) { CloseHandle(hMap);       hMap  = IntPtr.Zero; }
        }
    }

    public enum DisplayPage { Speed, DriveTime, DailyWeekly, JobInfo, Violations }

    public class TachographForm : Form
    {
        TelemetryReader reader;
        EU561Engine     engine = new EU561Engine();
        Timer           ticker = new Timer();
        Ets2SdkData     data   = new Ets2SdkData();
        DisplayPage     page   = DisplayPage.Speed;
        DateTime        lastTick;

        static readonly Color BG        = Color.FromArgb(30, 30, 30);
        static readonly Color PanelDark = Color.FromArgb(22, 22, 22);
        static readonly Color LcdBG     = Color.FromArgb(18, 22, 18);
        static readonly Color LcdGreen  = Color.FromArgb(140, 210, 140);
        static readonly Color LcdDim    = Color.FromArgb(35, 55, 35);
        static readonly Color LcdWhite  = Color.FromArgb(230, 230, 230);
        static readonly Color BtnFace   = Color.FromArgb(50, 50, 50);
        static readonly Color BtnBorder = Color.FromArgb(80, 80, 80);
        static readonly Color BtnText   = Color.FromArgb(200, 200, 200);
        static readonly Color AccentRed = Color.FromArgb(220, 60, 60);
        static readonly Color VdoBlue   = Color.FromArgb(80, 140, 220);

        Rectangle btnBack, btnUp, btnDown, btnOk, btnD1, btnD2, btnPrint;

        public TachographForm()
        {
            Text            = "VDO DTCO – ETS2 Tachograph";
            ClientSize      = new Size(760, 240);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = BG;
            DoubleBuffered  = true;
            StartPosition   = FormStartPosition.CenterScreen;

            TachoCache.Load(engine);

            reader   = new TelemetryReader();
            lastTick = DateTime.Now;

            ticker.Interval = 100;
            ticker.Tick    += OnTick;
            ticker.Start();

            this.MouseClick += OnMouseClick;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            ticker.Stop();
            TachoCache.Save(engine);
            reader?.Dispose();
            base.OnFormClosed(e);
        }

        void OnTick(object s, EventArgs e)
        {
            var now = DateTime.Now;
            float dt = (float)(now - lastTick).TotalSeconds;
            lastTick = now;

            if (reader.Connected)
            {
                data = reader.Read();
                engine.Tick(data.SpeedKmh > 1f && data.paused == 0, dt);
            }
            Invalidate();
        }

        void OnMouseClick(object s, MouseEventArgs e)
        {
            if (btnBack.Contains(e.Location))  { page = (DisplayPage)(((int)page - 1 + 5) % 5); Invalidate(); }
            if (btnUp.Contains(e.Location))    { page = (DisplayPage)(((int)page - 1 + 5) % 5); Invalidate(); }
            if (btnDown.Contains(e.Location))  { page = (DisplayPage)(((int)page + 1) % 5);      Invalidate(); }
            if (btnOk.Contains(e.Location))    { engine.Reset(); TachoCache.Save(engine); Invalidate(); }
            if (btnPrint.Contains(e.Location)) { DoPrint(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.TextRenderingHint  = TextRenderingHint.AntiAliasGridFit;

            int W = ClientSize.Width, H = ClientSize.Height;

            DrawRoundRect(g, new Rectangle(2, 2, W-4, H-4), 8, BG, Color.FromArgb(55,55,55), 1.5f);

            using var logoFont = new Font("Arial", 9f, FontStyle.Bold);
            g.DrawString("VDO", logoFont, new SolidBrush(Color.White), 14, 8);
            g.DrawString("⊛", new Font("Arial", 7f), new SolidBrush(Color.FromArgb(180,180,180)), 46, 9);
            g.DrawString("ʙ", new Font("Arial", 7f, FontStyle.Bold), new SolidBrush(VdoBlue), 390, 8);

            var lcd = new Rectangle(12, 24, 390, 110);
            DrawRoundRect(g, lcd, 4, LcdBG, Color.FromArgb(40,60,40), 1f);

            using var dimBrush = new SolidBrush(LcdDim);
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 28; col++)
                    g.FillRectangle(dimBrush, lcd.X+8 + col*13, lcd.Y+6 + row*24, 10, 18);

            DrawLcdContent(g, lcd);

            var cardBtn = new Rectangle(408, 24, 28, 110);
            DrawRoundRect(g, cardBtn, 4, PanelDark, Color.FromArgb(55,55,55), 1f);
            using var arrowFont = new Font("Arial", 11f);
            g.DrawString("▲", arrowFont, new SolidBrush(Color.FromArgb(90,90,90)), 411, 60);

            var cardArea = new Rectangle(442, 18, 300, 122);
            DrawRoundRect(g, cardArea, 6, PanelDark, Color.FromArgb(55,55,55), 1.5f);
            var slot = new Rectangle(458, 50, 265, 38);
            DrawRoundRect(g, slot, 3, Color.FromArgb(15,15,15), Color.FromArgb(70,70,70), 1f);
            using var slotFont = new Font("Arial", 8f);
            g.DrawString("DRIVER 1", slotFont, new SolidBrush(Color.FromArgb(80,80,80)), 530, 63);

            DrawScrew(g, 10, 10);
            DrawScrew(g, W-16, 10);
            DrawScrew(g, 10, H-16);
            DrawScrew(g, W-16, H-16);

            int bY = 148, bH = 32, bW = 58;
            btnBack  = new Rectangle(14,  bY, bW, bH);
            btnUp    = new Rectangle(78,  bY, bW, bH);
            btnDown  = new Rectangle(142, bY, bW, bH);
            btnOk    = new Rectangle(206, bY, bW+10, bH);

            DrawButton(g, btnBack,  "↩");
            DrawButton(g, btnUp,    "▲");
            DrawButton(g, btnDown,  "▼");
            DrawButton(g, btnOk,    "OK");

            btnD1 = new Rectangle(320, bY, 44, bH);
            btnD2 = new Rectangle(370, bY, 44, bH);
            DrawIconButton(g, btnD1, "D1");
            DrawIconButton(g, btnD2, "D2");

            btnPrint = new Rectangle(W-120, bY, 100, bH);
            DrawButton(g, btnPrint, "PRINT");

            var strip1 = new Rectangle(14,  H-42, 290, 22);
            var strip2 = new Rectangle(W-120, H-42, 100, 22);
            DrawRoundRect(g, strip1, 3, PanelDark, Color.FromArgb(45,45,45), 1f);
            DrawRoundRect(g, strip2, 3, PanelDark, Color.FromArgb(45,45,45), 1f);

            DrawStatusStrip(g, strip1);

            using var fineFont = new Font("Courier New", 7.5f);
            string fineStr = engine.TotalFines > 0
                ? $"FINE: €{engine.TotalFines:0}"
                : "EU 561/2006";
            var fineBrush = engine.TotalFines > 0
                ? new SolidBrush(AccentRed)
                : new SolidBrush(Color.FromArgb(80,80,80));
            g.DrawString(fineStr, fineFont, fineBrush, strip2.X+6, strip2.Y+5);
        }

        void DrawLcdContent(Graphics g, Rectangle lcd)
        {
            bool connected = reader.Connected;

            for (int i = 0; i < 5; i++)
            {
                var dotBrush = i == (int)page
                    ? new SolidBrush(LcdGreen)
                    : new SolidBrush(LcdDim);
                g.FillEllipse(dotBrush, lcd.X + 8 + i*10, lcd.Y + 96, 6, 6);
            }

            if (!connected)
            {
                DrawLcdText(g, lcd, "NO SIGNAL", 0, 20, 18f, true);
                DrawLcdText(g, lcd, "START ETS2", 0, 55, 11f, false);
                DrawLcdText(g, lcd, "PLUGIN REQUIRED", 0, 75, 9f, false);
                return;
            }

            switch (page)
            {
                case DisplayPage.Speed:       DrawPageSpeed(g, lcd);      break;
                case DisplayPage.DriveTime:   DrawPageDriveTime(g, lcd);  break;
                case DisplayPage.DailyWeekly: DrawPageWeekly(g, lcd);     break;
                case DisplayPage.JobInfo:     DrawPageJob(g, lcd);        break;
                case DisplayPage.Violations:  DrawPageViolations(g, lcd); break;
            }
        }

        void DrawPageSpeed(Graphics g, Rectangle lcd)
        {
            float spd = data.SpeedKmh;
            string spdStr = ((int)spd).ToString();
            using var bigFont   = new Font("Courier New", 38f, FontStyle.Bold);
            using var unitFont  = new Font("Courier New", 14f, FontStyle.Bold);
            using var smallFont = new Font("Courier New", 8.5f);
            var greenBrush = new SolidBrush(LcdGreen);

            bool overLimit = data.SpeedLimitKmh > 5f && spd > data.SpeedLimitKmh + 3f;
            var spdBrush = overLimit ? new SolidBrush(AccentRed) : greenBrush;

            g.DrawString(spdStr, bigFont, spdBrush, lcd.X + 180, lcd.Y + 10);
            g.DrawString("km/h", unitFont, greenBrush, lcd.X + 305, lcd.Y + 32);

            string gearStr = data.gearDashboard == 0 ? "N"
                           : data.gearDashboard < 0  ? "R"
                           : data.gearDashboard.ToString();
            using var gearFont = new Font("Courier New", 22f, FontStyle.Bold);
            g.DrawString(gearStr, gearFont, new SolidBrush(LcdGreen), lcd.X + 12, lcd.Y + 20);

            float rpmPct = data.engineRpmMax > 0 ? data.engineRpm / data.engineRpmMax : 0f;
            DrawBar(g, lcd.X+10, lcd.Y+68, 180, 8, rpmPct, LcdGreen, "RPM");

            float fuelPct = data.fuelCapacity > 0 ? data.fuel / data.fuelCapacity : 0f;
            var fuelColor = fuelPct < 0.15f ? AccentRed : LcdGreen;
            DrawBar(g, lcd.X+10, lcd.Y+82, 180, 8, fuelPct, fuelColor, "FUEL");

            if (data.SpeedLimitKmh > 5f)
                g.DrawString($"LIM {(int)data.SpeedLimitKmh}", smallFont,
                    new SolidBrush(overLimit ? AccentRed : LcdDim), lcd.X+305, lcd.Y+60);

            if (engine.BreakRequired)
                DrawLcdText(g, lcd, "! BREAK NOW", 0, 84, 9f, false, AccentRed);
            else if (engine.BreakDueIn < 30*60f)
                DrawLcdText(g, lcd, $"BRK {EU561Engine.Fmt(engine.BreakDueIn)}", 230, 84, 8.5f, false);
        }

        void DrawPageDriveTime(Graphics g, Rectangle lcd)
        {
            using var labelFont = new Font("Courier New", 8f);
            using var valFont   = new Font("Courier New", 16f, FontStyle.Bold);
            var wb = new SolidBrush(LcdWhite);
            var db = new SolidBrush(LcdDim);
            var rb = new SolidBrush(AccentRed);
            var gb = new SolidBrush(LcdGreen);

            g.DrawString("DRIVE TIME", labelFont, db, lcd.X+8, lcd.Y+6);

            g.DrawString("CONT", labelFont, wb, lcd.X+8, lcd.Y+26);
            g.DrawString(EU561Engine.Fmt(engine.ContinuousDrive), valFont,
                engine.BreakRequired ? rb : gb, lcd.X+55, lcd.Y+20);

            g.DrawString("BRK DUE", labelFont, wb, lcd.X+8, lcd.Y+52);
            g.DrawString(EU561Engine.Fmt(engine.BreakDueIn), valFont,
                engine.BreakDueIn < 0 ? rb : gb, lcd.X+75, lcd.Y+46);

            g.DrawString("REST", labelFont, wb, lcd.X+8, lcd.Y+78);
            g.DrawString(EU561Engine.Fmt(engine.CurrentRest), valFont, gb, lcd.X+55, lcd.Y+72);

            bool driving = data.SpeedKmh > 1f && data.paused == 0;
            g.DrawString(driving ? "> DRIVING" : "= RESTING",
                new Font("Courier New", 9f, FontStyle.Bold),
                driving ? gb : new SolidBrush(VdoBlue),
                lcd.X+230, lcd.Y+75);
        }

        void DrawPageWeekly(Graphics g, Rectangle lcd)
        {
            using var labelFont = new Font("Courier New", 8f);
            using var valFont   = new Font("Courier New", 14f, FontStyle.Bold);
            var wb = new SolidBrush(LcdWhite);
            var db = new SolidBrush(LcdDim);
            var gb = new SolidBrush(LcdGreen);

            g.DrawString("DAILY / WEEKLY", labelFont, db, lcd.X+8, lcd.Y+6);

            g.DrawString("DAILY DRIVE", labelFont, wb, lcd.X+8, lcd.Y+26);
            g.DrawString(EU561Engine.Fmt(engine.DailyDrive), valFont, gb, lcd.X+110, lcd.Y+20);

            g.DrawString("DAILY REM.", labelFont, wb, lcd.X+8, lcd.Y+46);
            g.DrawString(EU561Engine.Fmt(engine.DailyRemaining), valFont,
                new SolidBrush(engine.DailyRemaining < 3600f ? AccentRed : LcdGreen),
                lcd.X+110, lcd.Y+40);

            g.DrawString("WEEKLY DRV", labelFont, wb, lcd.X+8, lcd.Y+66);
            g.DrawString(EU561Engine.Fmt(engine.WeeklyDrive), valFont, gb, lcd.X+110, lcd.Y+60);

            g.DrawString("WEEKLY REM", labelFont, wb, lcd.X+8, lcd.Y+86);
            g.DrawString(EU561Engine.Fmt(engine.WeeklyRemaining), valFont,
                new SolidBrush(engine.WeeklyRemaining < 5*3600f ? AccentRed : LcdGreen),
                lcd.X+110, lcd.Y+80);
        }

        void DrawPageJob(Graphics g, Rectangle lcd)
        {
            using var labelFont = new Font("Courier New", 8f);
            using var valFont   = new Font("Courier New", 11f, FontStyle.Bold);
            var wb = new SolidBrush(LcdWhite);
            var db = new SolidBrush(LcdDim);
            var gb = new SolidBrush(LcdGreen);

            g.DrawString("JOB INFO", labelFont, db, lcd.X+8, lcd.Y+6);

            if (data.onJob == 0)
            {
                g.DrawString("NO ACTIVE JOB", valFont, db, lcd.X+80, lcd.Y+42);
                return;
            }

            g.DrawString("FROM",  labelFont, wb, lcd.X+8, lcd.Y+26);
            g.DrawString(data.CityFrom.Length > 0 ? data.CityFrom : "---", valFont, gb, lcd.X+55, lcd.Y+22);

            g.DrawString("TO",    labelFont, wb, lcd.X+8, lcd.Y+46);
            g.DrawString(data.CityTo.Length > 0 ? data.CityTo : "---", valFont, gb, lcd.X+55, lcd.Y+42);

            g.DrawString("DIST",  labelFont, wb, lcd.X+8, lcd.Y+66);
            g.DrawString($"{data.routeDistance/1000f:0.0} km", valFont, gb, lcd.X+55, lcd.Y+62);

            g.DrawString("TRUCK", labelFont, wb, lcd.X+8, lcd.Y+86);
            g.DrawString(data.Truck.Length > 0 ? data.Truck : "---", valFont, gb, lcd.X+55, lcd.Y+82);

            g.DrawString($"ODO {data.truckOdometer:0} km", labelFont,
                new SolidBrush(LcdDim), lcd.X+240, lcd.Y+86);
        }

        void DrawPageViolations(Graphics g, Rectangle lcd)
        {
            using var labelFont = new Font("Courier New", 8f);
            using var valFont   = new Font("Courier New", 10f, FontStyle.Bold);
            var rb = new SolidBrush(AccentRed);
            var gb = new SolidBrush(LcdGreen);
            var wb = new SolidBrush(LcdWhite);
            var db = new SolidBrush(LcdDim);

            g.DrawString("VIOLATIONS / FINES", labelFont, db, lcd.X+8, lcd.Y+6);

            if (!engine.IsViolation)
            {
                g.DrawString("NO VIOLATIONS", valFont, gb, lcd.X+80, lcd.Y+42);
                g.DrawString("DRIVE LEGALLY", labelFont, wb, lcd.X+100, lcd.Y+62);
            }
            else
            {
                g.DrawString("!! VIOLATION", valFont, rb, lcd.X+8, lcd.Y+26);
                g.DrawString(engine.LastViolation, labelFont, wb, lcd.X+8, lcd.Y+46);
                g.DrawString($"TOTAL FINES: EUR {engine.TotalFines:0}", valFont, rb, lcd.X+8, lcd.Y+66);
                g.DrawString("Press OK to reset", labelFont, wb, lcd.X+8, lcd.Y+86);
            }
        }

        void DrawStatusStrip(Graphics g, Rectangle r)
        {
            using var f = new Font("Courier New", 7.5f);
            bool driving = reader.Connected && data.SpeedKmh > 1f && data.paused == 0;

            string status = reader.Connected
                ? (driving ? $">  {data.SpeedKmh:0} km/h  |  ODO {data.truckOdometer:0} km"
                           : $"=  RESTING  |  ODO {data.truckOdometer:0} km")
                : "NO CONNECTION";

            g.DrawString(status, f, new SolidBrush(Color.FromArgb(100,100,100)), r.X+6, r.Y+5);
        }

        void DrawButton(Graphics g, Rectangle r, string label)
        {
            DrawRoundRect(g, r, 5, BtnFace, BtnBorder, 1f);
            using var f = new Font("Arial", 8.5f, FontStyle.Bold);
            var sz = g.MeasureString(label, f);
            g.DrawString(label, f, new SolidBrush(BtnText),
                r.X + (r.Width  - sz.Width)  / 2f,
                r.Y + (r.Height - sz.Height) / 2f);
        }

        void DrawIconButton(Graphics g, Rectangle r, string label)
        {
            DrawRoundRect(g, r, 5, BtnFace, BtnBorder, 1f);
            using var f = new Font("Arial", 8f);
            var sz = g.MeasureString(label, f);
            g.DrawString(label, f, new SolidBrush(BtnText),
                r.X + (r.Width  - sz.Width)  / 2f,
                r.Y + (r.Height - sz.Height) / 2f);
        }

        void DrawBar(Graphics g, int x, int y, int w, int h, float pct, Color col, string label)
        {
            pct = Math.Max(0f, Math.Min(1f, pct));
            g.FillRectangle(new SolidBrush(LcdDim), x, y, w, h);
            g.FillRectangle(new SolidBrush(col), x, y, (int)(w * pct), h);
            using var f = new Font("Courier New", 6.5f);
            g.DrawString(label, f, new SolidBrush(LcdDim), x, y - 10);
        }

        void DrawLcdText(Graphics g, Rectangle lcd, string text, int offX, int offY,
                         float size, bool center, Color? col = null)
        {
            using var f = new Font("Courier New", size, FontStyle.Bold);
            var brush = new SolidBrush(col ?? LcdGreen);
            if (center)
            {
                var sz = g.MeasureString(text, f);
                g.DrawString(text, f, brush, lcd.X + (lcd.Width - sz.Width)/2f, lcd.Y + offY);
            }
            else
            {
                g.DrawString(text, f, brush, lcd.X + offX, lcd.Y + offY);
            }
        }

        void DrawRoundRect(Graphics g, Rectangle r, int radius, Color fill, Color border, float bw)
        {
            using var path = RoundedRect(r, radius);
            using var fb   = new SolidBrush(fill);
            g.FillPath(fb, path);
            using var pen  = new Pen(border, bw);
            g.DrawPath(pen, path);
        }

        void DrawScrew(Graphics g, int cx, int cy)
        {
            g.FillEllipse(new SolidBrush(Color.FromArgb(40,40,40)), cx-4, cy-4, 8, 8);
            g.DrawEllipse(new Pen(Color.FromArgb(65,65,65), 1f), cx-4, cy-4, 8, 8);
            g.DrawLine(new Pen(Color.FromArgb(55,55,55), 0.8f), cx-2, cy, cx+2, cy);
            g.DrawLine(new Pen(Color.FromArgb(55,55,55), 0.8f), cx, cy-2, cx, cy+2);
        }

        static GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, rad*2, rad*2, 180, 90);
            path.AddArc(r.Right-rad*2, r.Y, rad*2, rad*2, 270, 90);
            path.AddArc(r.Right-rad*2, r.Bottom-rad*2, rad*2, rad*2, 0, 90);
            path.AddArc(r.X, r.Bottom-rad*2, rad*2, rad*2, 90, 90);
            path.CloseFigure();
            return path;
        }

        static readonly string[] FakePrinterKeywords = {
            "pdf", "xps", "fax", "onenote", "microsoft print",
            "bullzip", "dopdf", "cutepdf", "pdfcreator", "nitro",
            "foxit", "adobe pdf", "print to", "send to", "image writer",
            "generic", "virtual", "document writer"
        };

        static bool IsRealPrinter(string name)
        {
            string lower = name.ToLowerInvariant();
            foreach (var kw in FakePrinterKeywords)
                if (lower.Contains(kw)) return false;
            return true;
        }

        void DoPrint()
        {
            string exeDir  = AppDomain.CurrentDomain.BaseDirectory;
            string txtPath = Path.Combine(exeDir, $"tacho_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var sb = new StringBuilder();
            sb.AppendLine("===================================");
            sb.AppendLine("   VDO DTCO TACHOGRAPH PRINTOUT   ");
            sb.AppendLine("===================================");
            sb.AppendLine($"Date/Time : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Truck     : {(data.Truck.Length > 0 ? data.Truck : "---")}");
            sb.AppendLine($"Odometer  : {data.truckOdometer:0} km");
            sb.AppendLine();
            sb.AppendLine("--- EU 561/2006 TIMERS ------------");
            sb.AppendLine($"Continuous drive : {EU561Engine.Fmt(engine.ContinuousDrive)}");
            sb.AppendLine($"Break due in     : {EU561Engine.Fmt(engine.BreakDueIn)}");
            sb.AppendLine($"Daily drive      : {EU561Engine.Fmt(engine.DailyDrive)}");
            sb.AppendLine($"Daily remaining  : {EU561Engine.Fmt(engine.DailyRemaining)}");
            sb.AppendLine($"Weekly drive     : {EU561Engine.Fmt(engine.WeeklyDrive)}");
            sb.AppendLine($"Weekly remaining : {EU561Engine.Fmt(engine.WeeklyRemaining)}");
            sb.AppendLine($"Current rest     : {EU561Engine.Fmt(engine.CurrentRest)}");
            sb.AppendLine();
            sb.AppendLine("--- VIOLATIONS --------------------");
            if (engine.IsViolation)
            {
                sb.AppendLine($"!! {engine.LastViolation}");
                sb.AppendLine($"TOTAL FINES: EUR {engine.TotalFines:0}");
            }
            else
            {
                sb.AppendLine("No violations recorded.");
            }
            sb.AppendLine("===================================");

            try
            {
                File.WriteAllText(txtPath, sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save printout file:\n{ex.Message}",
                    "VDO Tachograph", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var realPrinters = new List<string>();
            foreach (string p in PrinterSettings.InstalledPrinters)
                if (IsRealPrinter(p)) realPrinters.Add(p);

            if (realPrinters.Count == 0)
            {
                MessageBox.Show(
                    $"No physical printer found.\nPrintout saved to:\n{txtPath}",
                    "VDO Tachograph", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string chosenPrinter;
            if (realPrinters.Count == 1)
            {
                chosenPrinter = realPrinters[0];
            }
            else
            {
                using var dlg = new PrinterPickerDialog(realPrinters);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                chosenPrinter = dlg.SelectedPrinter;
            }

            string vbsPath = Path.Combine(exeDir, "tacho_print.vbs");
            try
            {
                string vbs = $"Dim oShell\r\n" +
                             $"Set oShell = CreateObject(\"WScript.Shell\")\r\n" +
                             $"oShell.Run \"notepad.exe /p \"\"{txtPath}\"\"\", 0, False\r\n";
                File.WriteAllText(vbsPath, vbs);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "wscript.exe",
                    Arguments       = $"\"{vbsPath}\"",
                    CreateNoWindow  = true,
                    WindowStyle     = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);

                MessageBox.Show($"Sent to printer: {chosenPrinter}",
                    "VDO Tachograph", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print failed:\n{ex.Message}\nFile saved: {txtPath}",
                    "VDO Tachograph", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var pathToDelete = txtPath;
            var vbsToDelete  = vbsPath;
            var t = new System.Threading.Thread(() =>
            {
                System.Threading.Thread.Sleep(3 * 60 * 1000);
                try { File.Delete(pathToDelete); } catch { }
                try { File.Delete(vbsToDelete);  } catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TachographForm());
        }
    }

    public class PrinterPickerDialog : Form
    {
        public string SelectedPrinter { get; private set; } = "";
        ListBox list;

        public PrinterPickerDialog(List<string> printers)
        {
            Text            = "Select Printer";
            ClientSize      = new Size(360, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(30, 30, 30);

            var label = new Label
            {
                Text      = "Select a physical printer:",
                ForeColor = Color.FromArgb(200, 200, 200),
                BackColor = Color.Transparent,
                Location  = new Point(12, 10),
                Size      = new Size(336, 20)
            };

            list = new ListBox
            {
                Location    = new Point(12, 34),
                Size        = new Size(336, 110),
                BackColor   = Color.FromArgb(22, 22, 22),
                ForeColor   = Color.FromArgb(140, 210, 140),
                BorderStyle = BorderStyle.FixedSingle
            };
            foreach (var p in printers) list.Items.Add(p);
            if (list.Items.Count > 0) list.SelectedIndex = 0;

            var btnOk = new Button
            {
                Text      = "Print",
                Location  = new Point(190, 158),
                Size      = new Size(76, 28),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat
            };
            btnOk.Click += (s, e) =>
            {
                if (list.SelectedItem == null) return;
                SelectedPrinter = list.SelectedItem.ToString();
                DialogResult = DialogResult.OK;
                Close();
            };

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Location  = new Point(272, 158),
                Size      = new Size(76, 28),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { label, list, btnOk, btnCancel });
        }
    }
}
