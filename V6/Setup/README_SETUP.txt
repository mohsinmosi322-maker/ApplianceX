ApplianceX - Setup folder
=========================

Files
-----
  SETUP.bat              Run as Administrator. Creates DB + connectionstring.txt
  database.sql           Full schema + seed (admin / admin123)
  connectionstring.txt   Generated after SETUP.bat succeeds

Notes
-----
- SQL Server 2008 installer cannot be shipped legally here.
  Install SQL Server Express yourself first (2012 or newer recommended).
- SETUP.bat uses Windows Authentication (Integrated Security).
- Default login after DB create:  admin  /  admin123
