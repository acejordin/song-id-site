using ManagedBass;

public class RecordingDevice : IDisposable
{
    DeviceInfo _deviceInfo;

    public int Index { get; }
    public DeviceInfo DeviceInfo
    {
        get { return _deviceInfo; }
    }
    public RecordingDevice(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var recordingDevice = Enumerate().First(rd => rd.ToString() == name);
            Index = recordingDevice.Index;
            _deviceInfo = recordingDevice.DeviceInfo;
        }
        else if (Enumerate().Count() > 0)
        {
            _deviceInfo = Enumerate().First().DeviceInfo;
            Index = 0; //Default to first audio device
        }
        else
            throw new Exception("No audio devices enumerated");
    }

    public RecordingDevice(int index, DeviceInfo deviceInfo)
    {
        Index = index;

        _deviceInfo = deviceInfo;
    }

    public static IEnumerable<RecordingDevice> Enumerate()
    {
        for (int i = 0; Bass.RecordGetDeviceInfo(i, out var info); ++i)
            yield return new RecordingDevice(i, info);
    }

    public void Dispose()
    {
        Bass.CurrentRecordingDevice = Index;
        Bass.RecordFree();
    }

    public override string ToString() => $"{_deviceInfo.Name}, Type: {_deviceInfo.Type}, Driver: {_deviceInfo.Driver}, IsDefault: {_deviceInfo.IsDefault}";
}
