using System.Diagnostics.Metrics;

namespace emc.camus.api.test.Helpers;

internal static class MeterCaptureBuilder
{
    public static (MeterListener Listener, Func<long> GetValue) CreateListener(string instrumentName)
    {
        long recordedValue = 0;
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == instrumentName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            recordedValue = measurement;
        });
        listener.Start();
        return (listener, () => recordedValue);
    }
}
