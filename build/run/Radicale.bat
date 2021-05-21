echo "RUN ASC.Radicale"
call dotnet run --project ..\..\common\services\ASC.Radicale\ASC.Radicale.csproj --no-build --$STORAGE_ROOT=..\..\Data  --log__dir=..\..\Logs --log__name=radicale