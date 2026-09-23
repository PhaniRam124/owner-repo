using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DirectorFamilyTravelDesk.Models;

public sealed class LookupItem { public long Id { get; set; } public string Name { get; set; } = ""; public override string ToString()=>Name; }
public sealed class CityItem { public long Id { get; set; } public string Name { get; set; } = ""; public string Iata { get; set; } = ""; public bool IsActive { get; set; } public string Display => $"{Name} - {Iata}"; }
public sealed class TravellerItem { public long Id { get; set; } public string ShortCode { get; set; }=""; public string? FullName { get; set; } public string? RelationGroup { get; set; } public bool IsActive { get; set; } public string Display => string.IsNullOrWhiteSpace(FullName) ? ShortCode : $"{ShortCode} — {FullName}"; }
public sealed class RouteItem { public long Id { get; set; } public long FromCityId { get; set; } public long ToCityId { get; set; } public string RouteCode { get; set; }=""; public string RouteLabel { get; set; }=""; public string FromCity { get; set; }=""; public string ToCity { get; set; }=""; public bool IsActive { get; set; } public override string ToString()=>RouteLabel; }
public sealed class FlightItem { public long Id { get; set; } public string Airline { get; set; }=""; public string FlightNo { get; set; }=""; public long RouteId { get; set; } public string RouteCode { get; set; }=""; public string DepartureTime { get; set; }=""; public string ArrivalTime { get; set; }=""; public int Stops { get; set; } public string? ViaIata { get; set; } public string DisplayValue { get; set; }=""; public bool IsActive { get; set; } public override string ToString()=>DisplayValue; }
public sealed class TripListItem
{
    public long Id { get; set; } public string TripCode { get; set; }=""; public DateTime DepartureDate { get; set; } public DateTime? ReturnDate { get; set; }
    public long RouteId { get; set; } public string RouteLabel { get; set; }=""; public string Travellers { get; set; }=""; public int Pax { get; set; }
    public string? OnwardFlight { get; set; } public string? OnwardPnr { get; set; } public string? OnwardSeats { get; set; }
    public string? ReturnFlight { get; set; } public string? ReturnPnr { get; set; } public string? ReturnSeats { get; set; }
    public string? Hotel { get; set; } public string? Transport { get; set; } public string? TransportDetails { get; set; }
    public decimal? TotalCost { get; set; } public string? Status { get; set; } public string? NextAction { get; set; } public string? Remarks { get; set; }
    public string SelectorLabel => $"{DepartureDate:dd-MMM} · {RouteLabel.Replace(" → "," ⇄ ")} · {Pax} Pax · {TripCode}";
}
public sealed class TripEditModel
{
    public long Id { get; set; } public string TripCode { get; set; }=""; public DateTime DepartureDate { get; set; }=DateTime.Today; public DateTime? ReturnDate { get; set; }
    public long TripTypeId { get; set; } public long RouteId { get; set; } public long? OnwardFlightId { get; set; } public long? ReturnFlightId { get; set; }
    public string? OnwardPnr { get; set; } public string? OnwardSeats { get; set; } public string? ReturnPnr { get; set; } public string? ReturnSeats { get; set; }
    public long? HotelOptionId { get; set; } public long? TransportOptionId { get; set; } public string? TransportDetails { get; set; } public decimal? TotalCost { get; set; }
    public long? StatusOptionId { get; set; } public string? Remarks { get; set; } public List<long> TravellerIds { get; set; }=new();
}
public sealed class TravellerChoice : INotifyPropertyChanged
{
    public long Id { get; set; } public string Display { get; set; }=""; private bool _isSelected;
    public bool IsSelected { get=>_isSelected; set { if(_isSelected==value)return; _isSelected=value; PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(IsSelected))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}