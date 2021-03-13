<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Resume_Parser._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <head>
        <h1 align="center">Resume Parser / Career Optimizer</h1>
    </head>
    <body>
        <div class="container" style="margin: 0 auto; margin-top: 0%; text-align: justify; text-align: center;">   
            <asp:Label ID="headLabel" runat="server" Text="Resume Upload<br />(pdf/docx)<br />" Visible ="true"></asp:Label>

            <asp:FileUpload ID ="Uploader" runat="server" Style="display: inline !important;" Width="190px"/>
            <p></p>
            <p></p>
            <asp:Label ID="Label1" runat="server" Text="What position is this resume for?" Visible ="true"></asp:Label>
            
            <p>
                <asp:TextBox ID="positionText" runat="server" placeholder="ex: Software Engineer" Width="165px"></asp:TextBox>
            </p>

            <asp:Label ID="Label2" runat="server" Text="In what area?" Visible ="true"></asp:Label>
            
            <p>
                <asp:TextBox ID="locText" runat="server" placeholder="ex: Seattle, WA"></asp:TextBox>
            </p>

            <asp:Button ID="runButton" runat="server" OnClick="runButton_Click" Text="Parse"/>
            <p></p>
            <p>
                <asp:Label ID="mainLabel" runat="server" Text="Label" Visible ="false"></asp:Label>
            </p>
            <p>
                <asp:Label ID="ParserSectionLabel" runat="server" Text="What Recruiters See" Visible="false"></asp:Label>
            </p>
            <p>
                <asp:Label ID="ContactInfoLabel" runat="server" Text="Label" Visible="false" style="font-size: large"></asp:Label>
                <asp:Table ID="ContactInfoTable" runat="server" HorizontalAlign="Center">
                </asp:Table>
            </p>
            <p>
                <asp:Label ID="EducationLabel" runat="server" Text="Label" Visible="false" style="font-size: large"></asp:Label>
                <asp:Table ID="EducationTable" runat="server" HorizontalAlign="Center">
                </asp:Table>
            </p>
            <p>
                <asp:Label ID="ExperienceLabel" runat="server" Text="Label" Visible="false" style="font-size: large"></asp:Label>
                <asp:Table ID="ExperienceTable" runat="server" HorizontalAlign="Center">
                </asp:Table>
            </p>
            <p>
                <asp:Label ID="SkillsLabel" runat="server" Text="Label" Visible="false" style="font-size: large"></asp:Label>
                <asp:Table ID="SkillsTable" runat="server" HorizontalAlign="Center">
                </asp:Table>
            </p>
            <p>
                <asp:Label ID="SectionsLabel" runat="server" Visible="false" Text="Label" style="font-size: large"></asp:Label>
                <asp:Table ID="SectionsTable" runat="server" HorizontalAlign="Center">
                </asp:Table>
            </p>
        </div>

        <p><br /><br /></p>

        <div class="container" style="margin: 0 auto; margin-top: 0%; text-align: justify; text-align: center;">
            <p align="center">
                <asp:Label ID="ResumeChanges" runat="server" Text="Common Resume Sections<br/>" Visible ="false" ></asp:Label>
                <asp:Label ID="ChangeList" runat="server" Text="" Visible ="false" ></asp:Label>
            </p>  
        </div>

        <p><br /><br /></p>

        <div class="container" style="margin: 0 auto; margin-top: 0%; text-align: justify; text-align: center;">
            <p align="center">
                <asp:Label ID="JobLabel" runat="server" Text="Related Job Listings<br/>" Visible ="false" ></asp:Label>
                <asp:Label ID="noList" runat="server" Text="" Visible ="false" ></asp:Label>
            </p>
        </div>

        <div class="container" style="margin-top: 0%; margin-left: 30%; margin-right: 30%; text-align: justify; text-align: center;">
            <p align="left">
                <asp:Label ID="JobList" runat="server" Text="" Visible ="false"></asp:Label>
            </p>
        </div>
    </body>
</asp:Content>
