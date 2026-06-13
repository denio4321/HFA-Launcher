# HFA Launcher

Launcher de escritorio para **HFA** (Habbo Fútbol Argentina), construido con [Avalonia](https://avaloniaui.net/)
(.NET 10). Permite elegir la fuente de cliente — **AIR (Official)**, **AIR Plus** o **AIR HFA** —,
descargar/actualizar el cliente Habbo AIR y lanzarlo con el código de inicio del portapapeles.

Proyecto basado en el trabajo de **LilithRainbows** ([HabboCustomLauncher](https://github.com/LilithRainbows/HabboCustomLauncher)).
La fuente **AIR HFA** descarga el cliente ya parcheado desde el repo de distribución
[HfaPlusSwf](https://github.com/denio4321/HfaPlusSwf).

## Compilar

Requiere el SDK de **.NET 10** (ver `global.json`).

```powershell
dotnet build HabboCustomLauncher.csproj -c Debug
dotnet run   --project HabboCustomLauncher.csproj
```

## Publicar (release manual)

El RID de Windows es **win-x86** a propósito: corre tanto en Windows de 32 como de 64 bits y es el
único con el que el `.csproj` embebe los `.zip` de parche AIR de Windows (condición `win-x86`/`Debug`).

```powershell
dotnet publish HabboCustomLauncher.csproj -c Release -r win-x86 --self-contained true -o publish
```

## Releases automáticas

Hay un workflow de GitHub Actions (`.github/workflows/release.yml`) que, **al hacer push de un tag
`v*`**, compila el launcher y crea una Release con el zip adjunto:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

También se puede lanzar a mano desde la pestaña **Actions → Release → Run workflow**.

---

Repo: <https://github.com/denio4321/HFA-Launcher>
