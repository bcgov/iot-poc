# iot-poc (MOTI iot-poc project)
![img](https://img.shields.io/badge/Lifecycle-Stable-97ca00)

## Introduction
The IOT Proof of Concept project implements a small-scale Internet of Things (IoT) platform to demonstrate the feasibility and value of using a centralized system to manage the collection, storage, and sharing of sensor data. The project will pilot the use of Azure Cloud services as the IoT platform to evaluate the vendor's capabilities, better understand what functionality is required, learn what practices to apply, and gain insights on how end-to-end business solutions can be implemented in the Ministry using IoT Cloud services.
The application is being developed as an open source solution.

## Prerequisites

- .Net 5 or 6 SDK
- Microsoft Azure subscription

## Dependencies

- NA

## Repository Map

- **camera**: The iot edge solution to communicate MoTI ONVIF cameras
- **weather**: The iot edge solution to communicate MoTI weather sensors
- **MotiCameraApp**: The client web applicaion for displaying camera images/videos
- **iotFunctions**: Azure function project to resize/watermark images and provide APIs for external consumers

## Installation

This application is meant to be deployed to Microsoft Azure platform. The full application will require sufficient permission to work with Azure resources.

**DevOps**

CI/CD pipelines are not set up currently.

## Development

**camera**

- Azure iot edge module that runs on a virtual machine that used as an iot edge device to communicate with ONVIF cameras and Azure iot central.

**weather**

- Azure iot edge module that runs on a virtual machine that used as an iot edge device to communicate with MoTI weather sensors and Azure iot central.

**MotiCameraApp**

This application is a basic .NET core web application to display images/videos captured from the MoTI ONVIF cameras.

**iotFunctions**

- This project is based on .NET 6, so be sure to have the right .NET version during the development.

- Use the following urls for testing the API endpoints provided by Azure functions 
    Get weather telemetry
    - https://moti-iot-test.azurewebsites.net/api/weather/devices/iotcentral/35094/telemetries?dateFrom=2022-06-23&dateTo=2022-06-24

    Get camera telemetry
    - https://moti-iot-test.azurewebsites.net/api/camera/devices/iotcentral/1/imagedata?dateFrom=2022-07-05&dateTo=2022-07-05

    Get image
    - https://moti-iot-test.azurewebsites.net/api/images?imagePath=https://blobstorageformediatest.blob.core.windows.net/$web/images/axis-accc8eee60d0/West_653/axis-accc8eee60d0-West_653-1656534665.jpg

    Get property
    - https://moti-iot-test.azurewebsites.net/api/weather/devices/iotcentral/52391/property

## Contribution

Please report any [issues](https://github.com/bcgov/iot-poc/issues).

[Pull requests](https://github.com/bcgov/iot-poc/pulls) are always welcome.

If you would like to contribute, please see our [contributing](CONTRIBUTING.md) guidelines.

Please note that this project is released with a [Contributor Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project you agree to abide by its terms.

## License

    Copyright 2017 Province of British Columbia

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

## Maintenance

This repository is maintained by [BC Ministry of Transportation](http://www.th.gov.bc.ca/).
Click [here](https://github.com/orgs/bcgov/teams/tran/repositories) for a complete list of our repositories on GitHub. 

