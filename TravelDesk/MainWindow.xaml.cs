using System.Windows;
using DirectorFamilyTravelDesk.Views;

namespace DirectorFamilyTravelDesk;

public partial class MainWindow : Window
{
    private readonly DashboardView _dashboard=new();
    private readonly TripsView _trips=new();
    private readonly FlightsView _flights=new();
    private readonly HotelsView _hotels=new();
    private readonly CabsView _cabs=new();
    private readonly TravellersView _travellers=new();
    private readonly CitiesView _cities=new();
    private readonly RoutesView _routes=new();
    private readonly BackupView _backup=new();

    public MainWindow(){InitializeComponent();Show(_dashboard);}
    private void Show(object view){MainContent.Content=view;if(view is IRefreshable r)r.RefreshData();}
    private void Dashboard_Click(object s,RoutedEventArgs e)=>Show(_dashboard);
    private void Trips_Click(object s,RoutedEventArgs e)=>Show(_trips);
    private void Flights_Click(object s,RoutedEventArgs e)=>Show(_flights);
    private void Hotels_Click(object s,RoutedEventArgs e)=>Show(_hotels);
    private void Cabs_Click(object s,RoutedEventArgs e)=>Show(_cabs);
    private void Travellers_Click(object s,RoutedEventArgs e)=>Show(_travellers);
    private void Cities_Click(object s,RoutedEventArgs e)=>Show(_cities);
    private void Routes_Click(object s,RoutedEventArgs e)=>Show(_routes);
    private void Backup_Click(object s,RoutedEventArgs e)=>Show(_backup);
}