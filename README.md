# Resume Optimizer

## Table of Contents
* [Project Description](#project-description)
* [Features](#features)
* [Video: Resume Optimizer in Action](#video)
* [Authors](#authors)

<a name="#project-description"></a>

## Project Description
Resume Optimizer is utility application for job applicants to increase their chances of landing their dream job through **resume parsing**, **resume optimization**, and **job searching** tailored for their specific skills and career field. 

This application can be deployed as a web application using Azure Cloud Services with integrations built in to the .NET framework (and Visual Studio).

## Tools used 
* ASP.NET
* C# 
* Azure App Service
* Azure Blob Storage
* Azure SQL Server DB
* Affinda Resume Parser API
* Indeed Job Search API

<a name="#features"></a>

## Features

### Resume Parsing
Resume Optimizer makes use of Affinda's Resume Parser RESTful API to upload and parse resumes for critical applicant data that would be seen an in [Applicant Tracking System (ATS)](https://en.wikipedia.org/wiki/Applicant_tracking_system) used by recruiters to track and filter job applicants. Any information that is missing or could not be automatically parsed will be denoted in the application's parser output display.

### Resume Optimization
Using crowd-sourced data from the resumes of other job applicants using this application, Resume Optimizer allows for users to see the top 10 most common resume sections for their respective field and whether or not their resume contains those sections. This is dynamically updated upon each resume parse to update the frequency of each section across many different resumes.

### Job Searching
Resume Optimizer also enables users to automatically search for the top job listings for their field in their specified location -- allowing them to quickly tweak their resumes and then apply to viable positions in their field. Job data is pulled from Indeed's Job Search API and displayed on this application's webpage.

<a name="#video"></a>

## Video: Resume Optimizer In Action
<br>

<p align="center">
    <img align=center src="https://media.giphy.com/media/P764kqXfpG6eiz9jDS/giphy.gif">
</p>

<a name="#authors"></a>

## Authors
Jorge Alvarez ([@J-Alv](https://github.com/J-Alv))
<br>
Kolby Samson ([@kosamson](https://github.com/kosamson))
