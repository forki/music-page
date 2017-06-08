FROM fsharp

COPY . /app
WORKDIR /app

RUN mono .paket/paket.bootstrapper.exe
RUN mono .paket/paket.exe install

EXPOSE 8080
CMD ["fsharpi", "server.fsx"]
