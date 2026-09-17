UPDATE Halls 
SET Description = REPLACE(Description, '"supportedFormats":["IMAX"]', '"supportedFormats":["IMAX","3D","2D"]') 
WHERE HallId = 2058;

UPDATE Halls 
SET Description = REPLACE(Description, '"supportedFormats":["3D"]', '"supportedFormats":["2D","3D"]') 
WHERE HallId = 2059;
