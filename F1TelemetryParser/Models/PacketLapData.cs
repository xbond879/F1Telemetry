namespace F1UdpParser.Models;

public class PacketLapData : BasePacketData
{
    public uint   LastLapTimeInMs;	       	 // Last lap time in milliseconds
    public uint   CurrentLapTimeInMs; 	 // Current time around the lap in milliseconds
    public ushort Sector1TimeMsPart;         // Sector 1 time milliseconds part
    public byte   Sector1TimeMinutesPart;    // Sector 1 whole minute part
    public ushort Sector2TimeMsPart;         // Sector 2 time milliseconds part
    public byte   Sector2TimeMinutesPart;    // Sector 2 whole minute part
    public ushort DeltaToCarInFrontMsPart;   // Time delta to car in front milliseconds part
    public byte   DeltaToCarInFrontMinutesPart; // Time delta to car in front whole minute part
    public ushort DeltaToRaceLeaderMsPart;      // Time delta to race leader milliseconds part
    public byte   DeltaToRaceLeaderMinutesPart; // Time delta to race leader whole minute part
    public float  LapDistance;		 // Distance vehicle is around current lap in metres – could be negative if line hasn’t been crossed yet
    public float  TotalDistance;		 // Total distance travelled in session in metres – could be negative if line hasn’t been crossed yet
    public float  SafetyCarDelta;            // Delta in seconds for safety car
    public byte   CarPosition;   	         // Car race position
    public byte   CurrentLapNum;		 // Current lap number
    public byte   PitStatus;            	 // 0 = none, 1 = pitting, 2 = in pit area
    public byte   NumPitStops;            	 // Number of pit stops taken in this race
    public byte   Sector;               	 // 0 = sector1, 1 = sector2, 2 = sector3
    public byte   CurrentLapInvalid;    	 // Current lap invalid - 0 = valid, 1 = invalid
    public byte   Penalties;            	 // Accumulated time penalties in seconds to be added
    public byte   TotalWarnings;             // Accumulated number of warnings issued
    public byte   CornerCuttingWarnings;     // Accumulated number of corner cutting warnings issued
    public byte   NumUnservedDriveThroughPens;  // Num drive through pens left to serve
    public byte   NumUnservedStopGoPens;        // Num stop go pens left to serve
    public byte   GridPosition;         	 // Grid position the vehicle started the race in
    public byte   DriverStatus;         	 // Status of driver - 0 = in garage, 1 = flying lap  2 = in lap, 3 = out lap, 4 = on track
    public byte   ResultStatus;              // Result status - 0 = invalid, 1 = inactive, 2 = active 3 = finished, 4 = didnotfinish, 5 = disqualified 6 = not classified, 7 = retired
    public byte   PitLaneTimerActive;     	 // Pit lane timing, 0 = inactive, 1 = active
    public ushort PitLaneTimeInLaneInMs;   	 // If active, the current time spent in the pit lane in ms
    public ushort PitStopTimerInMs;        	 // Time of the actual pit stop in ms
    public byte   PitStopShouldServePen;   	 // Whether the car should serve a penalty at this stop
    public float  SpeedTrapFastestSpeed;     // Fastest speed through speed trap for this car in kmph
    public byte   SpeedTrapFastestLap;       // Lap no the fastest speed was achieved, 255 = not set
}