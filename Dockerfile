FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src 

COPY ["SplitTGBot/SplitTGBot.csproj", "SplitTGBot/"]
COPY ["TGBotClassLibrary/TGBotClassLibrary.csproj", "TGBotClassLibrary/"]
COPY ["SplitBotTests/SplitBotTests.csproj", "SplitBotTests/"]
RUN dotnet restore "SplitTGBot/SplitTGBot.csproj"

COPY . .

RUN dotnet test "SplitBotTests/SplitBotTests.csproj" --no-restore

WORKDIR "/src/SplitTGBot"
RUN dotnet publish "SplitTGBot.csproj" -c Release -o /app/publish


FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SplitTGBot.dll"]