using Microsoft.Data.Sqlite;
using DirectorFamilyTravelDesk.Models;
using System.Globalization;

namespace DirectorFamilyTravelDesk.Data;

public sealed partial class TravelRepository
{
    private bool ColumnExists(SqliteConnection c,string table,string column)
    {
        using var cmd=c.CreateCommand();
        cmd.CommandText=$"PRAGMA table_info({table})";
        using var r=cmd.ExecuteReader();
        while(r.Read()) if(string.Equals(r.GetString(1),column,StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void EnsureManagementSchema()
    {
        using var c=Open();
        if(!ColumnExists(c,"trips","booking_source"))
        {
            using var a=c.CreateCommand(); a.CommandText="ALTER TABLE trips ADD COLUMN booking_source TEXT"; a.ExecuteNonQuery();
        }
        if(!ColumnExists(c,"trips","booking_ref"))
        {
            using var a=c.CreateCommand(); a.CommandText="ALTER TABLE trips ADD COLUMN booking_ref TEXT"; a.ExecuteNonQuery();
        }

        using(var cmd=c.CreateCommand())
        {
            cmd.CommandText=@"
CREATE TABLE IF NOT EXISTS hotel_bookings(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 trip_id INTEGER NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
 hotel_name TEXT NOT NULL,
 city TEXT,
 check_in TEXT NOT NULL,
 check_out TEXT NOT NULL,
 booking_source TEXT,
 booking_ref TEXT,
 confirmation_no TEXT,
 room_type TEXT,
 amount REAL,
 status TEXT NOT NULL DEFAULT 'Booked',
 remarks TEXT,
 created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
 updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_hotel_trip ON hotel_bookings(trip_id);
CREATE INDEX IF NOT EXISTS idx_hotel_dates ON hotel_bookings(check_in,check_out);

CREATE TABLE IF NOT EXISTS cab_bookings(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 trip_id INTEGER NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
 travel_date TEXT NOT NULL,
 pickup_time TEXT,
 pickup_location TEXT NOT NULL,
 drop_location TEXT NOT NULL,
 provider TEXT,
 booking_source TEXT,
 booking_ref TEXT,
 vehicle_type TEXT,
 amount REAL,
 status TEXT NOT NULL DEFAULT 'Booked',
 remarks TEXT,
 created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
 updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_cab_trip ON cab_bookings(trip_id);
CREATE INDEX IF NOT EXISTS idx_cab_date ON cab_bookings(travel_date);";
            cmd.ExecuteNonQuery();
        }
    }

    public List<HotelBookingItem> GetHotelBookings(long? tripId=null)
    {
        using var c=Open(); using var cmd=c.CreateCommand();
        cmd.CommandText=@"SELECT h.id,h.trip_id,t.trip_code,h.hotel_name,COALESCE(h.city,''),h.check_in,h.check_out,
 COALESCE(h.booking_source,''),h.booking_ref,h.confirmation_no,h.room_type,h.amount,h.status,h.remarks
 FROM hotel_bookings h JOIN trips t ON t.id=h.trip_id"+
 (tripId.HasValue ? " WHERE h.trip_id=$tid" : "")+" ORDER BY h.check_in DESC,h.id DESC";
        if(tripId.HasValue) cmd.Parameters.AddWithValue("$tid",tripId.Value);
        using var r=cmd.ExecuteReader(); var a=new List<HotelBookingItem>();
        while(r.Read()) a.Add(new(){
            Id=r.GetInt64(0),TripId=r.GetInt64(1),TripCode=r.GetString(2),HotelName=r.GetString(3),City=r.GetString(4),
            CheckIn=DateTime.ParseExact(r.GetString(5),"yyyy-MM-dd",CultureInfo.InvariantCulture),
            CheckOut=DateTime.ParseExact(r.GetString(6),"yyyy-MM-dd",CultureInfo.InvariantCulture),
            BookingSource=r.GetString(7),BookingRef=r.IsDBNull(8)?null:r.GetString(8),ConfirmationNo=r.IsDBNull(9)?null:r.GetString(9),
            RoomType=r.IsDBNull(10)?null:r.GetString(10),Amount=r.IsDBNull(11)?null:r.GetDecimal(11),Status=r.GetString(12),
            Remarks=r.IsDBNull(13)?null:r.GetString(13)
        });
        return a;
    }

    public long SaveHotelBooking(HotelBookingItem x)
    {
        using var c=Open(); using var cmd=c.CreateCommand();
        if(x.Id==0) cmd.CommandText=@"INSERT INTO hotel_bookings(trip_id,hotel_name,city,check_in,check_out,booking_source,booking_ref,confirmation_no,room_type,amount,status,remarks)
 VALUES($t,$n,$c,$ci,$co,$bs,$br,$cn,$rt,$a,$s,$m); SELECT last_insert_rowid();";
        else { cmd.CommandText=@"UPDATE hotel_bookings SET trip_id=$t,hotel_name=$n,city=$c,check_in=$ci,check_out=$co,booking_source=$bs,booking_ref=$br,
 confirmation_no=$cn,room_type=$rt,amount=$a,status=$s,remarks=$m,updated_at=CURRENT_TIMESTAMP WHERE id=$id; SELECT $id"; cmd.Parameters.AddWithValue("$id",x.Id); }
        cmd.Parameters.AddWithValue("$t",x.TripId); cmd.Parameters.AddWithValue("$n",x.HotelName.Trim()); cmd.Parameters.AddWithValue("$c",x.City.Trim());
        cmd.Parameters.AddWithValue("$ci",x.CheckIn.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("$co",x.CheckOut.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$bs",(object?)x.BookingSource??DBNull.Value); cmd.Parameters.AddWithValue("$br",(object?)x.BookingRef??DBNull.Value);
        cmd.Parameters.AddWithValue("$cn",(object?)x.ConfirmationNo??DBNull.Value); cmd.Parameters.AddWithValue("$rt",(object?)x.RoomType??DBNull.Value);
        cmd.Parameters.AddWithValue("$a",(object?)x.Amount??DBNull.Value); cmd.Parameters.AddWithValue("$s",x.Status); cmd.Parameters.AddWithValue("$m",(object?)x.Remarks??DBNull.Value);
        return (long)(cmd.ExecuteScalar()??0L);
    }

    public void DeleteHotelBooking(long id){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="DELETE FROM hotel_bookings WHERE id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}

    public List<CabBookingItem> GetCabBookings(long? tripId=null)
    {
        using var c=Open(); using var cmd=c.CreateCommand();
        cmd.CommandText=@"SELECT b.id,b.trip_id,t.trip_code,b.travel_date,b.pickup_time,b.pickup_location,b.drop_location,b.provider,
 COALESCE(b.booking_source,''),b.booking_ref,b.vehicle_type,b.amount,b.status,b.remarks
 FROM cab_bookings b JOIN trips t ON t.id=b.trip_id"+
 (tripId.HasValue ? " WHERE b.trip_id=$tid" : "")+" ORDER BY b.travel_date DESC,b.pickup_time,b.id DESC";
        if(tripId.HasValue) cmd.Parameters.AddWithValue("$tid",tripId.Value);
        using var r=cmd.ExecuteReader(); var a=new List<CabBookingItem>();
        while(r.Read()) a.Add(new(){
            Id=r.GetInt64(0),TripId=r.GetInt64(1),TripCode=r.GetString(2),
            TravelDate=DateTime.ParseExact(r.GetString(3),"yyyy-MM-dd",CultureInfo.InvariantCulture),PickupTime=r.IsDBNull(4)?null:r.GetString(4),
            PickupLocation=r.GetString(5),DropLocation=r.GetString(6),Provider=r.IsDBNull(7)?null:r.GetString(7),BookingSource=r.GetString(8),
            BookingRef=r.IsDBNull(9)?null:r.GetString(9),VehicleType=r.IsDBNull(10)?null:r.GetString(10),Amount=r.IsDBNull(11)?null:r.GetDecimal(11),
            Status=r.GetString(12),Remarks=r.IsDBNull(13)?null:r.GetString(13)
        });
        return a;
    }

    public long SaveCabBooking(CabBookingItem x)
    {
        using var c=Open(); using var cmd=c.CreateCommand();
        if(x.Id==0) cmd.CommandText=@"INSERT INTO cab_bookings(trip_id,travel_date,pickup_time,pickup_location,drop_location,provider,booking_source,booking_ref,vehicle_type,amount,status,remarks)
 VALUES($t,$d,$pt,$p,$dr,$pr,$bs,$br,$v,$a,$s,$m); SELECT last_insert_rowid();";
        else { cmd.CommandText=@"UPDATE cab_bookings SET trip_id=$t,travel_date=$d,pickup_time=$pt,pickup_location=$p,drop_location=$dr,provider=$pr,
 booking_source=$bs,booking_ref=$br,vehicle_type=$v,amount=$a,status=$s,remarks=$m,updated_at=CURRENT_TIMESTAMP WHERE id=$id; SELECT $id"; cmd.Parameters.AddWithValue("$id",x.Id); }
        cmd.Parameters.AddWithValue("$t",x.TripId); cmd.Parameters.AddWithValue("$d",x.TravelDate.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("$pt",(object?)x.PickupTime??DBNull.Value);
        cmd.Parameters.AddWithValue("$p",x.PickupLocation.Trim()); cmd.Parameters.AddWithValue("$dr",x.DropLocation.Trim()); cmd.Parameters.AddWithValue("$pr",(object?)x.Provider??DBNull.Value);
        cmd.Parameters.AddWithValue("$bs",(object?)x.BookingSource??DBNull.Value); cmd.Parameters.AddWithValue("$br",(object?)x.BookingRef??DBNull.Value);
        cmd.Parameters.AddWithValue("$v",(object?)x.VehicleType??DBNull.Value); cmd.Parameters.AddWithValue("$a",(object?)x.Amount??DBNull.Value);
        cmd.Parameters.AddWithValue("$s",x.Status); cmd.Parameters.AddWithValue("$m",(object?)x.Remarks??DBNull.Value);
        return (long)(cmd.ExecuteScalar()??0L);
    }

    public void DeleteCabBooking(long id){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="DELETE FROM cab_bookings WHERE id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
}
