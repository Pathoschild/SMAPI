using System;
using SMAPI.Tests.ModApiConsumer.Interfaces;

namespace SMAPI.Tests.ModApiConsumer;

/// <summary>A simulated API consumer.</summary>
public class ApiConsumer
{
    /*********
    ** Public methods
    *********/
    /// <summary>Call the event field on the given API.</summary>
    /// <param name="api">The API to call.</param>
    /// <param name="getValues">Get the number of times the event was called and the last value received.</param>
    public void UseEventField(ISimpleApi api, out Func<(int timesCalled, int actualValue)> getValues)
    {
        // act
        int calls = 0;
        int lastValue = -1;
        api.OnEventRaised += (_, value) =>
        {
            calls++;
            lastValue = value;
        };

        getValues = () => (timesCalled: calls, actualValue: lastValue);
    }

    /// <summary>Call the event property on the given API.</summary>
    /// <param name="api">The API to call.</param>
    /// <param name="getValues">Get the number of times the event was called and the last value received.</param>
    public void UseEventProperty(ISimpleApi api, out Func<(int timesCalled, int actualValue)> getValues)
    {
        // act
        int calls = 0;
        int lastValue = -1;
        api.OnEventRaisedProperty += (_, value) =>
        {
            calls++;
            lastValue = value;
        };

        getValues = () => (timesCalled: calls, actualValue: lastValue);
    }
}
