USE master;
GO

IF DB_ID('TicoBusDB') IS NOT NULL
BEGIN
    ALTER DATABASE TicoBusDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TicoBusDB;
END
GO

CREATE DATABASE TicoBusDB;
GO

USE TicoBusDB;
GO

CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    NombreUsuario VARCHAR(50) NOT NULL UNIQUE,
    Clave VARCHAR(255) NOT NULL,
    Correo VARCHAR(100) NOT NULL UNIQUE,
    RolId INT NOT NULL,
    BloqueadoHasta DATETIME NULL,
    IntentosFallidos INT NOT NULL DEFAULT 0,
    FOREIGN KEY (RolId) REFERENCES Roles(Id)
);
GO

CREATE TABLE Choferes (
    Identificacion VARCHAR(30) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL,
    Apellidos VARCHAR(50) NOT NULL,
    UsuarioId INT NOT NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);
GO

CREATE TABLE Pasajeros (
    Identificacion VARCHAR(30) PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL,
    Apellidos VARCHAR(50) NOT NULL,
    UsuarioId INT NOT NULL,
    FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
);
GO

CREATE TABLE Rutas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Origen VARCHAR(100) NOT NULL,
    Destino VARCHAR(100) NOT NULL,
    DuracionEstimada TIME NOT NULL,
    PrecioBase DECIMAL(10,2) NOT NULL
);
GO

CREATE TABLE Unidades (
    Placa VARCHAR(30) PRIMARY KEY,
    Modelo VARCHAR(50) NOT NULL,
    AnioFabricacion INT NOT NULL,
    CapacidadPasajeros INT NOT NULL
);
GO

CREATE TABLE Viajes (
    NumeroViaje INT IDENTITY(1,1) PRIMARY KEY,
    RutaId INT NOT NULL,
    PlacaUnidad VARCHAR(30) NOT NULL,
    ChoferId VARCHAR(30) NOT NULL,
    FechaHoraSalida DATETIME NOT NULL,
    FechaHoraLlegadaEstimada DATETIME NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Programado',
    MotivoCancelacion VARCHAR(255) NULL,
    FOREIGN KEY (RutaId) REFERENCES Rutas(Id),
    FOREIGN KEY (PlacaUnidad) REFERENCES Unidades(Placa) ON UPDATE CASCADE,
    FOREIGN KEY (ChoferId) REFERENCES Choferes(Identificacion) ON UPDATE CASCADE
);
GO

CREATE TABLE Reservas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ViajeId INT NOT NULL,
    PasajeroId VARCHAR(30) NOT NULL,
    NumeroAsiento INT NOT NULL,
    MontoPagado DECIMAL(10,2) NOT NULL,
    FechaReserva DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (ViajeId) REFERENCES Viajes(NumeroViaje),
    FOREIGN KEY (PasajeroId) REFERENCES Pasajeros(Identificacion) ON UPDATE CASCADE,
    CONSTRAINT UQ_AsientoPorViaje UNIQUE (ViajeId, NumeroAsiento)
);
GO

INSERT INTO Roles (Nombre)
VALUES 
('Administrador'),
('Chofer'),
('Pasajero');
GO

INSERT INTO Usuarios (NombreUsuario, Clave, Correo, RolId)
VALUES 
('Administrador', 'TicoBus2025*', 'grupofirme94+admin@gmail.com', 1),
('chofer.mario', 'Chofer123*', 'grupofirme94+chofermario@gmail.com', 2),
('chofer.elena', 'Chofer456*', 'grupofirme94+choferelena@gmail.com', 2),
('pasajero.kevin', 'Pasa123*', 'grupofirme94+pasajerokevin@gmail.com', 3),
('pasajero.valeria', 'Pasa456*', 'grupofirme94+pasajerovaleria@gmail.com', 3);
GO

INSERT INTO Choferes (Identificacion, Nombre, Apellidos, UsuarioId)
VALUES 
('1-1111-1111', 'Mario', 'Alfaro Rojas', 2),
('2-2222-2222', 'Elena', 'Gómez Murillo', 3);
GO

INSERT INTO Pasajeros (Identificacion, Nombre, Apellidos, UsuarioId)
VALUES 
('3-3333-3333', 'Kevin', 'Solis Méndez', 4),
('4-4444-4444', 'Valeria', 'Chaves Mora', 5);
GO

INSERT INTO Rutas (Nombre, Origen, Destino, DuracionEstimada, PrecioBase)
VALUES 
('Ruta San José - Alajuela', 'San José', 'Alajuela', '00:45:00', 1200.00),
('Ruta San José - Liberia', 'San José', 'Liberia', '04:30:00', 5500.00),
('Ruta San José - Limón', 'San José', 'Limón', '03:15:00', 4200.00),
('Ruta Heredia - Cartago', 'Heredia', 'Cartago', '01:40:00', 2300.00);
GO

INSERT INTO Unidades (Placa, Modelo, AnioFabricacion, CapacidadPasajeros)
VALUES 
('SJ-B1234', 'Mercedes-Benz Marco Polo', 2022, 45),
('AL-B5678', 'Volvo Irizar i6', 2024, 50),
('G-B9012', 'Scania Touring', 2020, 40),
('HD-B7788', 'Yutong ZK6122H9', 2023, 48);
GO

INSERT INTO Viajes 
(RutaId, PlacaUnidad, ChoferId, FechaHoraSalida, FechaHoraLlegadaEstimada, Estado)
VALUES 
(1, 'SJ-B1234', '1-1111-1111', '2026-06-01 08:00:00', '2026-06-01 08:45:00', 'Programado'),
(2, 'AL-B5678', '2-2222-2222', '2026-06-01 14:00:00', '2026-06-01 18:30:00', 'En Curso'),
(3, 'G-B9012', '1-1111-1111', '2026-06-02 09:30:00', '2026-06-02 12:45:00', 'Programado'),
(4, 'HD-B7788', '2-2222-2222', '2026-06-02 06:30:00', '2026-06-02 08:10:00', 'En Curso');
GO

INSERT INTO Reservas
(ViajeId, PasajeroId, NumeroAsiento, MontoPagado)
VALUES
(2, '3-3333-3333', 12, 5500.00),
(2, '4-4444-4444', 15, 5500.00),
(4, '3-3333-3333', 7, 2300.00);
GO

SELECT 
    u.NombreUsuario,
    u.Clave,
    u.Correo,
    r.Nombre AS Rol
FROM Usuarios u
INNER JOIN Roles r ON u.RolId = r.Id
ORDER BY u.Id;
GO