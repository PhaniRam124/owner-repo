using System.Windows;
using System.Windows.Controls;
using DirectorFamilyTravelDesk.Models;

namespace DirectorFamilyTravelDesk.Views;

public partial class TravellersView : UserControl, IRefreshable
{
    private long _id;
    public TravellersView(){ InitializeComponent(); }
    public void RefreshData(){ Grid.ItemsSource=App.Repository.GetTravellers(false); }

    private void Grid_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if(Grid.SelectedItem is not TravellerItem x) return;
        _id=x.Id; CodeBox.Text=x.ShortCode; FullBox.Text=x.FullName; GroupBox.Text=x.RelationGroup; ActiveBox.IsChecked=x.IsActive;
        ModeText.Text=$"EDITING: {x.ShortCode}"; SaveButton.Content="Save Changes";
    }

    private void New_Click(object s, RoutedEventArgs e)=>ResetForm();

    private void ResetForm()
    {
        _id=0; Grid.SelectedItem=null; CodeBox.Text=""; FullBox.Text=""; GroupBox.Text="Family"; ActiveBox.IsChecked=true;
        ModeText.Text="NEW TRAVELLER"; SaveButton.Content="Save Traveller"; CodeBox.Focus();
    }

    private void Save_Click(object s, RoutedEventArgs e)
    {
        var code=CodeBox.Text.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(code)){ MessageBox.Show("Short code is required.","Travellers"); return; }
        if(App.Repository.TravellerExists(code,_id)){ MessageBox.Show("This traveller short code already exists.","Duplicate Traveller",MessageBoxButton.OK,MessageBoxImage.Information); return; }
        try{
            App.Repository.SaveTraveller(new(){Id=_id,ShortCode=code,FullName=FullBox.Text.Trim(),RelationGroup=GroupBox.Text.Trim(),IsActive=ActiveBox.IsChecked==true});
            RefreshData(); ResetForm();
        }catch(Exception ex){ MessageBox.Show(ex.Message,"Unable to save traveller",MessageBoxButton.OK,MessageBoxImage.Error); }
    }

    private void Delete_Click(object s, RoutedEventArgs e)
    {
        if(_id==0){ MessageBox.Show("Select a traveller to delete."); return; }
        if(MessageBox.Show($"Delete traveller '{CodeBox.Text}'?","Confirm Delete",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
        try{ App.Repository.DeleteTraveller(_id); RefreshData(); ResetForm(); }
        catch(Exception ex){ MessageBox.Show(ex.Message,"Traveller cannot be deleted",MessageBoxButton.OK,MessageBoxImage.Information); }
    }
}