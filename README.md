<div align="center">

# 🚌 Sistema TicoBus
### Sistema de Gestión de Transporte Interurbano
#### Universidad de Costa Rica

<br/>

![Status](https://img.shields.io/badge/Estado-En%20Desarrollo-0f7654?style=for-the-badge&logo=github)
![ASP.NET](https://img.shields.io/badge/ASP.NET-Core-512BD4?style=for-the-badge&logo=dotnet)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)

<br/>

> **Sistema de Gestión de Transporte** desarrollado como proyecto del curso  
> *Lenguajes para Aplicaciones Comerciales* — Universidad de Costa Rica

</div>

---

## 📋 Descripción

**TicoBus** es una aplicación web de gestión de transporte interurbano que permite administrar choferes, rutas, unidades y viajes. El sistema controla el ciclo completo de vida de cada viaje, desde su programación hasta su finalización, con notificaciones por correo electrónico en cada etapa.

---

## 🏗️ Arquitectura

```
SistemaTicoBus/
├── SistemaTicoBus.MODEL/      # Entidades y modelos de dominio
│   └── Entidades/             # Chofer, Pasajero, Ruta, Unidad, Viaje, Reserva
│
├── SistemaTicoBus.DA/         # Capa de acceso a datos
│   ├── Data/                  # AppDbContext (Entity Framework)
│   └── Repositorios/          # Acceso directo a SQL Server
│
├── SistemaTicoBus.BL/         # Capa de lógica de negocio
│   └── Servicios/             # Reglas de negocio y validaciones
│
└── SistemaTicoBus.WEB/        # Capa de presentación (ASP.NET MVC)
    ├── Controllers/           # Controladores por módulo
    ├── Models/                # ViewModels
    └── Views/                 # Vistas Razor con layout compartido
```

---

## 🛠️ Instalación y Configuración

### Pasos

**1. Clonar el repositorio**
```bash
git clone https://github.com/Gensis17/SistemaTicoBus.git
cd SistemaTicoBus
```

**2. Crear la base de datos**

Ejecutar el script SQL `TicoBusDB.sql` en SQL Server Management Studio para crear la base de datos y las tablas necesarias.

**3. Configurar la conexión**

Verificar el connection string en `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=TicoBusDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**4. Ejecutar el proyecto**
```bash
dotnet run --project SistemaTicoBus.WEB
```

### Credenciales por defecto

| Usuario | Clave | Rol |
|---------|-------|-----|
| Administrador | TicoBus2025* | Administrador |

---

## 👥 Equipo de Desarrollo

<div align="center">

| Nombre | Carné | Módulos | Primera Entrega |
|--------|-------|---------|
| David Daniel Sotela Sánchez | C17735 | Módulo 6 — Gestión de Viajes |
| Keity López Reyes | C34387 | Módulos 3, 4 y 9 — Pasajeros, Rutas y Mis Viajes |
| Génesis Gutiérrez Espinoza | C4F794 | Módulos 5 y 8 — Unidades y Viajes Cancelados |
| Royland Ruiz Arias | C06978 | Módulo 7 — Viajes en Curso |
| Diego Alejandro Quirós Clímaco | C06225 | Módulos 1 y 2 — Login, Choferes |

| Nombre | Carné | Módulos | Segunda Entrega |
|--------|-------|---------|
| David Daniel Sotela Sánchez | C17735 | Módulos 4 — Aplicacion Móvil MAUI |
| Keity López Reyes | C34387 | Módulo 3 — Publicacion en Azure |
| Génesis Gutiérrez Espinoza | C4F794 | Módulo 2 — API REST Segura con API Key |
| Royland Ruiz Arias | C06978 | Módulo 2 — API REST Segura con API Key |
| Diego Alejandro Quirós Clímaco | C06225 | Módulo 2 — API REST Segura con API Key |

</div>

---

## 📁 Buenas Prácticas Aplicadas

- ✅ Arquitectura en capas (MODEL / DA / BL / WEB)
- ✅ Layout compartido `_LayoutDashboard.cshtml` para todas las vistas
- ✅ CSS separado en `dashboard.css` con CSS nesting
- ✅ Conventional Commits (`feat:`, `fix:`, `refactor:`)
- ✅ Ramas por desarrollador (`modulo6-David`, etc.)
- ✅ Pull Requests para integración al master
- ✅ Validaciones en todas las capas
- ✅ Transacciones SQL para operaciones críticas
- ✅ Notificaciones por correo en eventos importantes

---

<div align="center">

**Universidad de Costa Rica**  
*Lenguajes para Aplicaciones Comerciales*  
2026

</div>
