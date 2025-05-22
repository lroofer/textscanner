# Set up guide

Use `docker-compose up --build` to start services, or use embedded to the Visual Studio Code launch system (pressing Run button)

## Usage

To upload file use POST http://localhost:3535/api/upload-file or prepared upload-file.html web page

Expected responses:

- 200 File was added
- 400 Incorrect Request or file

To download file use GET command http://localhost:3535/api/download-file/{id}

curl -v http://localhost:3535/api/download-file/f3d50fed-6ecf-45e0-a6bb-e14289243eeb -o downloaded_file.txt

To analyza file use GET command http://localhost:3535/api/analyze-file/{id}

It will return JSON as an answer

curl -v http://localhost:3535/api/analyze-file/