## Migrations
### Add a migration
```bash
 dotnet ef migrations add InitialCreate --project ShotYourPet.Migrations --startup-project ShotYourPet.Timeline
```
### Apply migrations
```bash
dotnet ef database update --project ShotYourPet.Migrations --startup-project ShotYourPet.Timeline
```