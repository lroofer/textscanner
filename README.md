# Text Scanner

## Overview

Text Scanner is a microservices-based application designed to upload, store, and analyze text files. The system consists of three main microservices:

1. **API Gateway**: The entry point for all client requests, routing them to appropriate services
2. **File Storing Service**: Handles file upload, storage, and downloading
3. **File Analysis Service**: Performs text analysis, calculating statistics such as paragraph count, word count, and character count

## Setup Guide

### Installation

1. Clone the repository:
```bash
git clone https://github.com/lroofer/textscanner.git
cd text-scanner```
2. Start the services using Docker Compose:
docker-compose up --build

The system exposes a REST API through the API Gateway at http://localhost:3535.
A Swagger UI interface is available at http://localhost:3535/swagger
You can get the postman configuration in Attachments/textChecker-postman-collection.json (more other demonstrating files are in Attachments)

## Usage

Upload a file to the system. The file must be sent as a multipart/form-data request.
To upload file use POST http://localhost:3535/api/upload-file or prepared upload-file.html web page

#### Example using HTML form:
Use the provided upload-file.html web page for interface.

#### Expected responses:

200 OK: File was successfully uploaded. Response contains the file ID.
304 Not Modified: File already exists in the system. Response contains the existing file ID.
400 Bad Request: Incorrect request or missing file.
500 Internal Server Error: Server-side error occurred.

To download file use GET command http://localhost:3535/api/download-file/{id}

curl -v http://localhost:3535/api/download-file/f3d50fed-6ecf-45e0-a6bb-e14289243eeb -o downloaded_file.txt

To analyze file use GET command http://localhost:3535/api/analyze-file/{id}

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
Implementation details of microservices are hidden from clients
Clients interact with a single endpoint