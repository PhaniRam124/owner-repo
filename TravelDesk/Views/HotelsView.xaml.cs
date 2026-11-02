using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DirectorFamilyTravelDesk.Models;

namespace DirectorFamilyTravelDesk.Views;

public partial class HotelsView : UserControl, IRefreshable
{
    private long _id;
    public HotelsView(){InitializeComponent();}
    public void RefreshData(){TripBox.ItemsSource=App.Repository.GetTrips();Grid.ItemsSource=App.Repository.GetHotelBookings();if(CheckInBox.SelectedDate==null)CheckInBox.SelectedDate=DateTime.Today;if(CheckOutBox.SelectedDate==null)CheckOutBox.SelectedDate=DateTime.Today.AddDays(1);}
    private void Reset(){_id=0;Grid.SelectedItem=null;TripBox.SelectedIndex=-1;HotelNameBox.Text=CityBox.Text=BookingRefBox.Text=ConfirmationBox.Text=RoomTypeBox.Text=AmountBox.Text="";SourceBox.Text="MakeMyTrip";CheckInBox.SelectedDate=DateTime.Today;CheckOutBox.SelectedDate=DateTime.Today.AddDays(1);ModeText.Text="NEW HOTEL BOOKING";SaveButton.Content="Save Hotel";}
    private void New_Click(object s,RoutedEventArgs e)=>Reset();
    private void Grid_SelectionChanged(object s,SelectionChangedEventArgs e){if(Grid.SelectedItem is not HotelBookingItem x)return;_id=x.Id;TripBox.SelectedValue=x.TripId;HotelNameBox.Text=x.HotelName;CityBox.Text=x.City;CheckInBox.SelectedDate=x.CheckIn;CheckOutBox.SelectedDate=x.CheckOut;SourceBox.Text=x.BookingSource;BookingRefBox.Text=x.BookingRef;ConfirmationBox.Text=x.ConfirmationNo;RoomTypeBox.Text=x.RoomType;AmountBox.Text=x.Amount?.ToString("0.##");ModeText.Text=$"EDITING: {x.HotelName}";SaveButton.Content="Save Changes";}
    private void Save_Click(object s,RoutedEventArgs e){if(TripBox.SelectedValue is not long tripId||string.IsNullOrWhiteSpace(HotelNameBox.Text)||CheckInBox.SelectedDate==null||CheckOutBox.SelectedDate==null){MessageBox.Show("Trip, hotel name, check-in and check-out are required.");return;}if(CheckOutBox.SelectedDate<CheckInBox.SelectedDate){MessageBox.Show("Check-out cannot be before check-in.");return;}decimal? amount=null;if(!string.IsNullOrWhiteSpace(AmountBox.Text)){if(!decimal.TryParse(AmountBox.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var v)){MessageBox.Show("Amount is not valid.");return;}amount=v;}App.Repository.SaveHotelBooking(new(){Id=_id,TripId=tripId,HotelName=HotelNameBox.Text.Trim(),City=CityBox.Text.Trim(),CheckIn=CheckInBox.SelectedDate.Value,CheckOut=CheckOutBox.SelectedDate.Value,BookingSource=SourceBox.Text.Trim(),BookingRef=BookingRefBox.Text.Trim(),ConfirmationNo=ConfirmationBox.Text.Trim(),RoomType=RoomTypeBox.Text.Trim(),Amount=amount,Status="Booked"});RefreshData();Reset();}
    private void Delete_Click(object s,RoutedEventArgs e){if(_id==0){MessageBox.Show("Select a hotel booking to delete.");return;}if(MessageBox.Show("Delete selected hotel booking?","Confirm",MessageBoxButton.YesNo,MessageBoxImage.Warning)==MessageBoxResult.Yes){App.Repository.DeleteHotelBooking(_id);RefreshData();Reset();}}
}