using System.Windows;
using System.Windows.Controls;
using DirectorFamilyTravelDesk.Models;

namespace DirectorFamilyTravelDesk.Views;

public partial class RoutesView : UserControl, IRefreshable
{
    private long _id;
    public RoutesView(){ InitializeComponent(); }

    public void RefreshData()
    {
        var cities=App.Repository.GetCities(false);
        FromCity.ItemsSource=cities; ToCity.ItemsSource=cities;
        Grid.ItemsSource=App.Repository.GetRoutes(false);
    }

    private void Grid_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if(Grid.SelectedItem is not RouteItem x) return;
        _id=x.Id; FromCity.SelectedValue=x.FromCityId; ToCity.SelectedValue=x.ToCityId; ActiveBox.IsChecked=x.IsActive;
        ModeText.Text=$"EDITING: {x.RouteLabel}"; SaveButton.Content="Save Changes";
    }

    private void New_Click(object s, RoutedEventArgs e)=>ResetForm();

    private void ResetForm()
    {
        _id=0; Grid.SelectedItem=null; FromCity.SelectedIndex=-1; ToCity.SelectedIndex=-1; ActiveBox.IsChecked=true;
        ModeText.Text="NEW ROUTE"; SaveButton.Content="Save Route";
    }

    private void Save_Click(object s, RoutedEventArgs e)
    {
        if(FromCity.SelectedValue is not long f || ToCity.SelectedValue is not long t || f==t){ MessageBox.Show("Select different From and To cities.","Routes"); return; }
        try{
            App.Repository.SaveRoute(new(){Id=_id,FromCityId=f,ToCityId=t,IsActive=ActiveBox.IsChecked==true});
            RefreshData(); ResetForm();
        }catch(Exception ex){ MessageBox.Show(ex.Message,"Unable to save route",MessageBoxButton.OK,MessageBoxImage.Error); }
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if(_id==0){ MessageBox.Show("Select a route to delete."); return; }
        if(MessageBox.Show("Delete the selected route?","Confirm Delete",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
        try{ App.Repository.DeleteRoute(_id); RefreshData(); ResetForm(); }
        catch(Exception ex){ MessageBox.Show(ex.Message,"Route cannot be deleted",MessageBoxButton.OK,MessageBoxImage.Information); }
    }
}