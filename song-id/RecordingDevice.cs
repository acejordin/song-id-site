using ManagedBass;

public class RecordingDevice : IDisposable
{
    string _name = "";

    public int Index { get; }

    public RecordingDevice(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _name = name;
            Index = RecordingDevice.Enumerate().First(rd => rd._name == name).Index;
        }
        else if (RecordingDevice.Enumerate().Count() > 0)
        {
            _name = RecordingDevice.Enumerate().First()._name;
            Index = 0; //Default o first audio device
        }
        else
            throw new Exception("No audio devices enumerated");
    }

    public RecordingDevice(int index, string name)
    {
        Index = index;

        _name = name;
    }

    public static IEnumerable<RecordingDevice> Enumerate()
    {
        for (int i = 0; Bass.RecordGetDeviceInfo(i, out var info); ++i)
            yield return new RecordingDevice(i, info.Name);
    }

    public void Dispose()
    {
        Bass.CurrentRecordingDevice = Index;
        Bass.RecordFree();
    }

    public override string ToString() => _name;
}
