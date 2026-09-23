using System.Windows;
using System.Windows.Controls;
using DirectorFamilyTravelDesk.Models;

namespace DirectorFamilyTravelDesk.Views;

public partial class CitiesView : UserControl, IRefreshable
{
    private long _id;
    public CitiesView(){ InitializeComponent(); }

    public void RefreshData(){ Grid.ItemsSource=App.Repository.GetCities(false); }

    private void Grid_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if(Grid.SelectedItem is not CityItem x) return;
        _id=x.Id; NameBox.Text=x.Name; IataBox.Text=x.Iata; ActiveBox.IsChecked=x.IsActive;
        ModeText.Text=$"EDITING: {x.Name}"; SaveButton.Content="Save Changes";
    }

    private void New_Click(object s, RoutedEventArgs e) => ResetForm();

    private void ResetForm()
    {
        _id=0; Grid.SelectedItem=null; NameBox.Text=""; IataBox.Text=""; ActiveBox.IsChecked=true;
        ModeText.Text="NEW CITY"; SaveButton.Content="Save City"; NameBox.Focus();
    }

    private void Save_Click(object s, RoutedEventArgs e)
    {
        var name=NameBox.Text.Trim(); var iata=IataBox.Text.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(name) || iata.Length!=3){ MessageBox.Show("Enter a city name and a 3-letter IATA code.","Cities"); return; }
        if(App.Repository.CityExists(name,iata,_id)){ MessageBox.Show("A city with the same name or IATA code already exists.","Duplicate City",MessageBoxButton.OK,MessageBoxImage.Information); return; }
        try{
            App.Repository.SaveCity(new(){Id=_id,Name=name,Iata=iata,IsActive=ActiveBox.IsChecked==true});
            RefreshData(); ResetForm();
        }catch(Exception ex){ MessageBox.Show(ex.Message,"Unable to save city",MessageBoxButton.OK,MessageBoxImage.Error); }
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if(_id==0){ MessageBox.Show("Select a city to delete."); return; }
        if(MessageBox.Show($"Delete city '{NameBox.Text}'?","Confirm Delete",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
        try{ App.Repository.DeleteCity(_id); RefreshData(); ResetForm(); }
        catch(Exception ex){ MessageBox.Show(ex.Message,"City cannot be deleted",MessageBoxButton.OK,MessageBoxImage.Information); }
    }
}