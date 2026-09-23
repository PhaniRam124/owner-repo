using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DirectorFamilyTravelDesk.Models;

namespace DirectorFamilyTravelDesk.Views;

public partial class FlightsView : UserControl, IRefreshable
{
    private long _id;
    private List<FlightItem> _all=new();
    public FlightsView(){ InitializeComponent(); }

    public void RefreshData()
    {
        var routes=App.Repository.GetRoutes(false);
        RouteBox.ItemsSource=routes;
        FilterRoute.ItemsSource=new[]{new RouteItem{Id=0,RouteLabel="All Routes"}}.Concat(routes).ToList();
        FilterRoute.SelectedValue=0L;
        _all=App.Repository.GetAllFlights();
        Grid.ItemsSource=_all;
    }

    private void FilterRoute_Changed(object s, SelectionChangedEventArgs e)
    {
        if(!IsLoaded) return;
        var id=FilterRoute.SelectedValue is long routeId ? routeId : 0L;
        Grid.ItemsSource=id==0 ? _all : _all.Where(x=>x.RouteId==id).ToList();
    }

    private void Grid_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if(Grid.SelectedItem is not FlightItem x) return;
        _id=x.Id; AirlineBox.Text=x.Airline; FlightNoBox.Text=x.FlightNo; RouteBox.SelectedValue=x.RouteId;
        DepBox.Text=x.DepartureTime; ArrBox.Text=x.ArrivalTime; StopsBox.Text=x.Stops.ToString(); ViaBox.Text=x.ViaIata;
        ActiveBox.IsChecked=x.IsActive; ModeText.Text=$"EDITING: {x.FlightNo}"; SaveButton.Content="Save Changes";
    }

    private void New_Click(object s, RoutedEventArgs e)=>ResetForm();

    private void ResetForm()
    {
        _id=0; Grid.SelectedItem=null; AirlineBox.Text=""; FlightNoBox.Text=""; DepBox.Text=""; ArrBox.Text=""; StopsBox.Text="0"; ViaBox.Text="";
        RouteBox.SelectedIndex=-1; ActiveBox.IsChecked=true; ModeText.Text="NEW FLIGHT"; SaveButton.Content="Save Flight"; AirlineBox.Focus();
    }

    private void Save_Click(object s, RoutedEventArgs e)
    {
        if(string.IsNullOrWhiteSpace(AirlineBox.Text) || string.IsNullOrWhiteSpace(FlightNoBox.Text) || RouteBox.SelectedValue is not long rid ||
           !TimeSpan.TryParseExact(DepBox.Text,@"hh\:mm",CultureInfo.InvariantCulture,out _) ||
           !TimeSpan.TryParseExact(ArrBox.Text,@"hh\:mm",CultureInfo.InvariantCulture,out _))
        { MessageBox.Show("Enter Airline, Flight No, Route and valid HH:mm times.","Flights"); return; }

        int.TryParse(StopsBox.Text,out var stops);
        try{
            App.Repository.SaveFlight(new(){Id=_id,Airline=AirlineBox.Text.Trim(),FlightNo=FlightNoBox.Text.Trim(),RouteId=rid,
                DepartureTime=DepBox.Text.Trim(),ArrivalTime=ArrBox.Text.Trim(),Stops=Math.Max(0,stops),
                ViaIata=ViaBox.Text.Trim().ToUpperInvariant(),IsActive=ActiveBox.IsChecked==true});
            RefreshData(); ResetForm();
        }catch(Exception ex){ MessageBox.Show(ex.Message,"Unable to save flight",MessageBoxButton.OK,MessageBoxImage.Error); }
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if(_id==0){ MessageBox.Show("Select a flight to delete."); return; }
        if(MessageBox.Show($"Delete flight '{FlightNoBox.Text}'?","Confirm Delete",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
        try{ App.Repository.DeleteFlight(_id); RefreshData(); ResetForm(); }
        catch(Exception ex){ MessageBox.Show(ex.Message,"Flight cannot be deleted",MessageBoxButton.OK,MessageBoxImage.Information); }
    }
}