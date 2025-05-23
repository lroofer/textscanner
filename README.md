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

I also attached folder "images-as-example" of the screenshots of bruno queries and postman collection

## Architecture

- api-gateway is a service that distributes queries to other microservices (it is one and only entry point for all the system)
- file-storing-service opperates with files
1. Uploading
2. Downloading
3. Getting meta data (meta data is a way to get some information about file before downloading it. I use it to prevent analyzing huge binary files)
- file-analysis-service analyzes file
It can store hash for the previously analyzed files and also analyze new files

You can get the postman configuration in Attachments/textChecker-postman-collection.json
Or use swagger at http://localhost:3535/swagger