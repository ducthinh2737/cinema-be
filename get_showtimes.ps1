$connectionString = 'Server=(localdb)\mssqllocaldb;Database=CinemaBookingDb;Trusted_Connection=True;'
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = '
    SELECT s.ShowtimeId, s.StartTime, m.Title, h.HallName
    FROM Showtimes s
    JOIN Movies m ON s.MovieId = m.MovieId
    JOIN Halls h ON s.HallId = h.HallId
    WHERE m.Title LIKE ''%Top Gun%''
'
$reader = $command.ExecuteReader()
while ($reader.Read()) {
    $id = $reader['ShowtimeId']
    $start = $reader['StartTime']
    $title = $reader['Title']
    $hallName = $reader['HallName']
    Write-Output "ShowtimeID: $id | StartTime: $start | Movie: $title | Hall: $hallName"
}
$connection.Close()
