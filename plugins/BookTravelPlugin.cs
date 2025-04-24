using Microsoft.SemanticKernel;
using System.ComponentModel;

public class BookTravelPlugin
{
    [KernelFunction]
    [Description("Book a flight to a destination")] 
    public string BookFlight(
        [Description("Destination city")] string destination,
        [Description("Departure date")] string date)
    {
        return $"Flight booked to {destination} on {date}.";
    }
}
