using ManagedBass;

public class RecordingDevice : IDisposable
{
    string _name;

    public int Index { get; }

    public RecordingDevice(string name)
    {
        _name = name;
        this.Index = RecordingDevice.Enumerate().First(rd => rd._name == name).Index;
    }

    public RecordingDevice(int index, string name)
    {
        this.Index = index;

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
