# AzTagger

AzTagger is a .NET 8 Windows Desktop GUI application built using WinForms. It is a helper tool that allows high-performance search and filtering of all Azure resources, resource groups, and subscriptions using Azure Resource Graph across all tenants and subscriptions that the signed-in user has permissions to access.

## Features

### User Authentication and Tenant Selection
- Use MSAL (Microsoft Authentication Library) for sign-in with Entra ID via web browser, including tenant selection and support for multi-factor authentication.
- Select the Azure Environment (e.g., `AzureCloud`, `AzureChinaCloud`) before querying for accessible tenants.
- Select from a list of all Entra ID tenants the user has access to in a drop-down list.
- Apply subsequent searches only to the selected tenant.
- Remember the selected tenant and Azure Environment upon exiting the application and reload them on the next start.

### Search Functionality
- Perform high-performance search and filtering of all Azure resources, resource groups, and subscriptions based on in-memory result data from Azure Resource Graph.
- Provide a single text input field for easy and flexible filtering of resources.

### Search Results Display
- Support large number of search results without slowing down interactions like scrolling.
- Enable column sorting and provide separate column-specific filter input fields above each column for quick filtering within that column's values.
- Double-clicking an item opens the Azure Portal GUI for that item in the system's default web browser.

### Tag Management
- Enable easy deletion of tags and inline editing of any item's key and value.
- Allow adding new tags by clicking into the last empty line's key or value cells and start typing.
- Store tag templates in a `tagtemplates.json` file in the user's AppData Local folder.
- Apply button to set or update all the specified tags on the selected subscriptions, resource groups, and resources.

### Application Settings and Environment
- Store user settings in a `settings.json` file in the user's AppData Local folder.

### Error Handling and Logging
- Log all errors via Serilog to an `errorlog.txt` file in the user's AppData Local folder.
- Provide user-friendly error messages in message boxes when errors occur.

### Performance and Responsiveness
- Ensure the application's responsiveness at all times by performing searches and tag updates on background threads.
- Using asynchronous programming patterns (`async`/`await`) for I/O-bound operations.

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
