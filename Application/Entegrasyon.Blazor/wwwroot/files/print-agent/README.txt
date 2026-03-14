Entegrasyon Print Agent

Bu dizine agent binary dosyalari yerlestirilmelidir.
Build komutu:

Windows:
  dotnet publish Agent/Entegrasyon.PrintAgent -c Release -r win-x64 --self-contained -o publish/win-x64
  (sonra zip'le ve buraya kopyala: EntegrasyonPrintAgent-win-x64.zip)

Linux:
  dotnet publish Agent/Entegrasyon.PrintAgent -c Release -r linux-x64 --self-contained -o publish/linux-x64
  (sonra zip'le ve buraya kopyala: EntegrasyonPrintAgent-linux-x64.zip)
