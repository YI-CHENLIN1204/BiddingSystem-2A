FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["崴正/崴正.csproj", "崴正/"]
RUN dotnet restore "崴正/崴正.csproj"

COPY ["崴正/", "崴正/"]
# Default Release artifact contains no test-only fault injection.
ARG FAULT_INJECTION_TEST=false
RUN if [ "$FAULT_INJECTION_TEST" = "true" ]; then \
      dotnet publish "崴正/崴正.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:DefineConstants=FAULT_INJECTION_TEST; \
    elif [ "$FAULT_INJECTION_TEST" = "false" ]; then \
      dotnet publish "崴正/崴正.csproj" -c Release -o /app/publish /p:UseAppHost=false; \
    else \
      echo "Invalid FAULT_INJECTION_TEST build argument" >&2; exit 1; \
    fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "崴正.dll"]
