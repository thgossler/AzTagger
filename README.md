<!-- SHIELDS -->
<div align="center">

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

</div>

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/thgossler/AzTagger">
    <img src="AzTagger/images/icon.png" alt="Icon" width="80" height="80">
  </a>

  <h1 align="center">AzTagger</h1>

  <p align="center">
    Query and filter your Azure resources fast and tag them easily.
    <br />
    <a href="https://github.com/thgossler/AzTagger/issues">Report Bug</a>
    ·
    <a href="https://github.com/thgossler/AzTagger/issues">Request Feature</a>
    ·
    <a href="https://github.com/thgossler/AzTagger#contributing">Contribute</a>
    ·
    <a href="https://github.com/sponsors/thgossler">Sponsor project</a>
    ·
    <a href="https://www.paypal.com/donate/?hosted_button_id=JVG7PFJ8DMW7J">Sponsor via PayPal</a>
  </p>
</div>

# Introduction

AzTagger is a Windows Desktop GUI application for fast and flexible querying of Azure resources 
and tag management. It is a helper tool that allows fast search and filtering of all resources, 
resource groups, and subscriptions using Azure Resource Graph for your Entra ID tenant.

<img src="./AzTagger/images/screenshot.jpg" alt="Screenshot" width="2049">

## Features

### User Authentication and Tenant Selection

- Interactive sign-in to Entra ID via web browser, support for multi-factor authentication
- Support for multiple contexts including Azure environment (e.g., AzurePublicCloud, AzureChina), Entra ID tenant and app ID

### Search Functionality

- Fast search and filtering of all Azure resources, resource groups, and subscriptions based on in-memory result data from Azure Resource Graph
- A single input field for easy and flexible querying of resources
- Multiple input fields for easy and flexible local quick-filtering of resources
- Comprehensive support of KQL and .NET regular expressions

### Search Results Display

- Support for large numbers of search results
- Column sorting
- Double-click on item to open it in the Azure Portal
- Context menu items to add result values to the search query for filtering
- Full display of all tags in tooltips

### Tag Management

- Easy inline editing and deletion of tags in a table
- Add new tags by clicking into the last empty line's key or value cells and start typing
- Use default and maintain custom tag templates in a `tagtemplates.json` file in the user's AppData Local folder
- Support of variables in tag template values such as {Date}, {Time}, {DateTime}, {User} 
- Create and update all specified tags on all selected subscriptions, resource groups, and resources at once

### Error Handling and Logging

- All errors logged to an `errorlog.txt` file in the user's AppData Local folder.

## Used Technologies

- C# .NET 9
- WinForms
- SeriLog
- Azure Identity
- Azure Resource Graph
- Azure Resource Manager

## Report Bugs

Please open an issue on the GitHub repository with the tag "bug".

## Donate

If you are using the tool but are unable to contribute technically, please consider promoting it and donating an amount that reflects its value to you. You can do so either via PayPal

[![Donate via PayPal](https://www.paypalobjects.com/en_US/i/btn/btn_donate_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=JVG7PFJ8DMW7J)

or via [GitHub Sponsors](https://github.com/sponsors/thgossler).

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star :wink: Thanks!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

<!-- MARKDOWN LINKS & IMAGES (https://www.markdownguide.org/basic-syntax/#reference-style-links) -->
[contributors-shield]: https://img.shields.io/github/contributors/thgossler/AzTagger.svg
[contributors-url]: https://github.com/thgossler/AzTagger/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/thgossler/AzTagger.svg
[forks-url]: https://github.com/thgossler/AzTagger/network/members
[stars-shield]: https://img.shields.io/github/stars/thgossler/AzTagger.svg
[stars-url]: https://github.com/thgossler/AzTagger/stargazers
[issues-shield]: https://img.shields.io/github/issues/thgossler/AzTagger.svg
[issues-url]: https://github.com/thgossler/AzTagger/issues
[license-shield]: https://img.shields.io/github/license/thgossler/AzTagger.svg
[license-url]: https://github.com/thgossler/AzTagger/blob/main/LICENSE
