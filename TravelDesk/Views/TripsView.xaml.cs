using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DirectorFamilyTravelDesk.Models;

namespace DirectorFamilyTravelDesk.Views;

public partial class TripsView : UserControl, IRefreshable
{
    private List<TripListItem> _all=new();
    public TripsView(){ InitializeComponent(); }

    public void RefreshData()
    {
        _all=App.Repository.GetTrips();
        StatusFilter.Items.Clear();
        StatusFilter.Items.Add("All Statuses");
        foreach(var s in App.Repository.GetLookup("status_options")) StatusFilter.Items.Add(s.Name);
        StatusFilter.SelectedIndex=0;
        Apply();
    }

    private void Apply()
    {
        var q=_all.AsEnumerable();
        var search=SearchBox?.Text?.Trim();
        if(!string.IsNullOrWhiteSpace(search))
            q=q.Where(x=>string.Join(' ',x.TripCode,x.RouteLabel,x.Travellers,x.OnwardPnr,x.ReturnPnr,x.Status).Contains(search,StringComparison.OrdinalIgnoreCase));
        var status=StatusFilter?.SelectedItem?.ToString();
        if(!string.IsNullOrWhiteSpace(status) && status!="All Statuses") q=q.Where(x=>x.Status==status);
        var rows=q.ToList();
        GridTrips.ItemsSource=rows;
        if(rows.Count>0) GridTrips.SelectedIndex=0; else ClearDetails();
    }

    private void Filter_Changed(object s, EventArgs e){ if(IsLoaded) Apply(); }

    private void GridTrips_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if(GridTrips.SelectedItem is not TripListItem x){ ClearDetails(); return; }
        OnwardFlightText.Text=x.OnwardFlight ?? "Not selected";
        OnwardMetaText.Text=$"PNR: {x.OnwardPnr ?? "—"}   •   Seats: {x.OnwardSeats ?? "—"}";
        ReturnFlightText.Text=x.ReturnFlight ?? "Not selected";
        ReturnMetaText.Text=$"PNR: {x.ReturnPnr ?? "—"}   •   Seats: {x.ReturnSeats ?? "—"}";
        TripMetaText.Text=$"{x.RouteLabel}   •   {x.Pax} Pax   •   {x.Transport ?? "Transport not set"}   •   {x.Status ?? "No status"}";
        RemarksText.Text=string.IsNullOrWhiteSpace(x.Remarks) ? (x.NextAction ?? "") : $"Next: {x.NextAction ?? "—"}   |   {x.Remarks}";
    }

    private void ClearDetails()
    {
        OnwardFlightText.Text="Select a trip"; OnwardMetaText.Text=""; ReturnFlightText.Text="—"; ReturnMetaText.Text=""; TripMetaText.Text=""; RemarksText.Text="";
    }

    private void New_Click(object s, RoutedEventArgs e)
    {
        var w=new TripEditorWindow{Owner=Window.GetWindow(this)};
        if(w.ShowDialog()==true) RefreshData();
    }

    private void EditSelected()
    {
        if(GridTrips.SelectedItem is not TripListItem x){ MessageBox.Show("Select a trip to edit."); return; }
        var w=new TripEditorWindow(x.Id){Owner=Window.GetWindow(this)};
        if(w.ShowDialog()==true) RefreshData();
    }

    private void Edit_Click(object s, RoutedEventArgs e)=>EditSelected();
    private void GridTrips_DoubleClick(object s, MouseButtonEventArgs e)=>EditSelected();

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if(GridTrips.SelectedItem is not TripListItem x){ MessageBox.Show("Select a trip to delete."); return; }
        if(MessageBox.Show($"Delete {x.TripCode}?","Confirm Delete",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes)
        { App.Repository.DeleteTrip(x.Id); RefreshData(); }
    }
}