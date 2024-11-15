# AzTagger

AzTagger is a Windows Desktop GUI application for fast and flexible querying of Azure resources 
and tag management. It is a helper tool that allows fast search and filtering of all resources, 
resource groups, and subscriptions using Azure Resource Graph for your Entra ID tenant.

## Features

### User Authentication and Tenant Selection

- MSAL (Microsoft Authentication Library) for sign-in with Entra ID via web browser support for multi-factor authentication
- Azure Environment (e.g., `AzureCloud`, `AzureChinaCloud`) and Entra ID tenant configured in settings file in the user's AppData Local folder

### Search Functionality

- Fast search and filtering of all Azure resources, resource groups, and subscriptions based on in-memory result data from Azure Resource Graph
- A single input field for easy and flexible querying of resources
- Multiple input fields for easy and flexible local quick-filtering of resources

### Search Results Display

- Support for large numbers of search results
- Column sorting
- Double-click on item to open it in the Azure Portal

### Tag Management

- Easy inline editing and deletion of tags in a table
- Add new tags by clicking into the last empty line's key or value cells and start typing
- Maintain tag templates in a `tagtemplates.json` file in the user's AppData Local folder
- Create and update all specified tags on the selected subscriptions, resource groups, and resources

### Error Handling and Logging

- All errors logged to an `errorlog.txt` file in the user's AppData Local folder.

## Ideas for Improvements

- Support placeholder variable in tag templates (e.g. for dynamic values like the current date)

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
