using System.Reflection;
using System.Text;
using F1UdpParser.Models;

namespace F1UdpParser;

public static class F1PackageParser
{
    public static async Task<BasePacketData?> ParsePackage(byte[] data)
    {
        //await File.WriteAllBytesAsync($"/tmp/samples/{DateTime.Now.Ticks}",  data);
        try
        {
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms, Encoding.Default);

            var headerFields = typeof(PacketHeader).GetFields(BindingFlags.Instance | BindingFlags.Public);
            var header = new PacketHeader();
            foreach (var field in headerFields)
            {
                if (!TryReadByType(field.FieldType, br, value => field.SetValue(header, value)))
                {
                    await Console.Error.WriteLineAsync($"Failed to read type: {field.FieldType}");
                }
            }

            switch (header.PacketId)
            {
                case PacketTypes.Session:
                    return ParsePacket<PacketSessionData>(br, header);
                case PacketTypes.LapData:
                    return ParsePacket<PacketLapData>(br, header, header.PlayerCarIndex);
                case PacketTypes.CarTelemetry:
                    return ParsePacket<PacketCarTelemetryData>(br, header, header.PlayerCarIndex);
                default:
                    //Console.WriteLine($"Skipping packet of type {header.m_packetId}");
                    break;
            }
        } 
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
        return null;
    }

    private static T ParsePacket<T>(BinaryReader br, PacketHeader header, byte playerCarIndex = 0) where T : BasePacketData, new()
    {
        var result = new T() { Header = header};
        var telemetryFields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
        while (playerCarIndex-- > 0)
        {
            foreach (var field in telemetryFields.Where(f => f.FieldType.Namespace == "System"))
            {
                TryReadByType(field.FieldType, br, _ => { });
            }
        }
        foreach (var field in telemetryFields.Where(f => f.FieldType.Namespace == "System"))
        {
            TryReadByType(field.FieldType, br, (val) => field.SetValue(result, val));
        }
        return result;
    }

    private static bool TryReadByType(Type fieldFieldType, BinaryReader br, Action<object?> setter)
    {
        try
        {
            switch (fieldFieldType.Name)
            {
                case "Byte":
                    setter(br.ReadByte());
                    break;
                case "Byte[]":
                    setter(new [] { br.ReadByte(), br.ReadByte(), br.ReadByte(), br.ReadByte()});
                    break;
                case "SByte":
                    setter(br.ReadSByte());
                    break;
                case "UInt16":
                    setter(br.ReadUInt16());
                    break;
                case "UInt16[]":
                    setter(new [] { br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt16()});
                    break;
                case "UInt32":
                    setter(br.ReadUInt32());
                    break;
                case "UInt64":
                    setter(br.ReadUInt64());
                    break;
                case "Single":
                    setter(br.ReadSingle());
                    break;
                case "Single[]":
                    setter(new [] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()});
                    break;
                default:
                    if (fieldFieldType.IsEnum)
                    {
                        setter(br.ReadByte());
                        break;
                    }
                    else
                    {
                        throw new NotImplementedException($"Type {fieldFieldType.Name} not supported yet.");
                    }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return false;
        }

        return true;
    }
}