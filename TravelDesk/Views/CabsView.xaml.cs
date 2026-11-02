using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DirectorFamilyTravelDesk.Models;

namespace DirectorFamilyTravelDesk.Views;

public partial class CabsView : UserControl, IRefreshable
{
    private long _id;
    public CabsView(){InitializeComponent();StatusBox.SelectedIndex=1;}
    public void RefreshData(){TripBox.ItemsSource=App.Repository.GetTrips();Grid.ItemsSource=App.Repository.GetCabBookings();if(DateBox.SelectedDate==null)DateBox.SelectedDate=DateTime.Today;}
    private void Reset(){_id=0;Grid.SelectedItem=null;TripBox.SelectedIndex=-1;DateBox.SelectedDate=DateTime.Today;TimeBox.Text=PickupBox.Text=DropBox.Text=ProviderBox.Text=BookingRefBox.Text=VehicleBox.Text=AmountBox.Text="";SourceBox.Text="MakeMyTrip";StatusBox.SelectedIndex=1;ModeText.Text="NEW CAB BOOKING";SaveButton.Content="Save Cab";}
    private void New_Click(object s,RoutedEventArgs e)=>Reset();
    private void Grid_SelectionChanged(object s,SelectionChangedEventArgs e){if(Grid.SelectedItem is not CabBookingItem x)return;_id=x.Id;TripBox.SelectedValue=x.TripId;DateBox.SelectedDate=x.TravelDate;TimeBox.Text=x.PickupTime;PickupBox.Text=x.PickupLocation;DropBox.Text=x.DropLocation;ProviderBox.Text=x.Provider;SourceBox.Text=x.BookingSource;BookingRefBox.Text=x.BookingRef;VehicleBox.Text=x.VehicleType;AmountBox.Text=x.Amount?.ToString("0.##");foreach(ComboBoxItem i in StatusBox.Items)if(i.Content?.ToString()==x.Status){StatusBox.SelectedItem=i;break;}ModeText.Text=$"EDITING: {x.Route}";SaveButton.Content="Save Changes";}
    private void Save_Click(object s,RoutedEventArgs e){if(TripBox.SelectedValue is not long tripId||DateBox.SelectedDate==null||string.IsNullOrWhiteSpace(PickupBox.Text)||string.IsNullOrWhiteSpace(DropBox.Text)){MessageBox.Show("Trip, date, pickup and drop are required.");return;}decimal? amount=null;if(!string.IsNullOrWhiteSpace(AmountBox.Text)){if(!decimal.TryParse(AmountBox.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var v)){MessageBox.Show("Amount is not valid.");return;}amount=v;}App.Repository.SaveCabBooking(new(){Id=_id,TripId=tripId,TravelDate=DateBox.SelectedDate.Value,PickupTime=TimeBox.Text.Trim(),PickupLocation=PickupBox.Text.Trim(),DropLocation=DropBox.Text.Trim(),Provider=ProviderBox.Text.Trim(),BookingSource=SourceBox.Text.Trim(),BookingRef=BookingRefBox.Text.Trim(),VehicleType=VehicleBox.Text.Trim(),Amount=amount,Status=(StatusBox.SelectedItem as ComboBoxItem)?.Content?.ToString()??"Booked"});RefreshData();Reset();}
    private void Delete_Click(object s,RoutedEventArgs e){if(_id==0){MessageBox.Show("Select a cab booking to delete.");return;}if(MessageBox.Show("Delete selected cab booking?","Confirm",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){App.Repository.DeleteCabBooking(_id);RefreshData();Reset();}}
}