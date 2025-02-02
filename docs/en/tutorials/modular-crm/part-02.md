# Creating the Initial Products Module

````json
//[doc-params]
{
    "UI": ["MVC","Blazor","BlazorServer", "BlazorWebApp","NG","MAUIBlazor"],
    "DB": ["EF","Mongo"]
}
````
````json
//[doc-nav]
{
  "Previous": {
    "Name": "Creating the initial solution",
    "Path": "tutorials/modular-crm/part-01"
  },
  "Next": {
    "Name": "Building the Products module",
    "Path": "tutorials/modular-crm/part-03"
  }
}
````

In this part, you will create a new product management module and install it in the main CRM application.

## Creating Solution Folders

You can create solution folders and sub-folders in *Solution Explorer* to organize your solution components better. Right-click to the solution root on the *Solution Explorer* panel, and select *Add* -> *New Folder* command:

![abp-studio-add-new-folder-command](images/abp-studio-add-new-folder-command.png)

That command opens a dialog where you can set the folder name:

![abp-studio-new-folder-dialog](images/abp-studio-new-folder-dialog.png)

Create a `main` and a `modules` folder using the *New Folder* command, then move the `ModularCrm` module into the `main` folder (simply by drag & drop). The *Solution Explorer* panel should look like the following figure now:

![abp-studio-solution-explorer-with-folders](images/abp-studio-solution-explorer-with-folders.png)

## Creating The Module

There are three module templates provided by ABP Studio:

* **Empty Module**: You can use that module template to build your module structure from scratch.
* **DDD Module**: A Domain Driven Design based layered module structure.
* **Standard Module**: A module template that is similar to the DDD module but without the domain layer.

We will use the *DDD Module* template for the Product module and the *Standard Module* template later in this tutorial.

Right-click the `modules` folder on the *Solution Explorer* panel, and select the *Add* -> *New Module* -> *DDD Module* command:

![abp-studio-add-new-ddd-module](images/abp-studio-add-new-ddd-module.png)

This command opens a new dialog to define the properties of the new module. You can use the following values to create a new module named `ModularCrm.Products`:

![abp-studio-create-new-module-dialog](images/abp-studio-create-new-module-dialog.png)

When you click the *Next* button, you are redirected to the UI selection step.

### Selecting the UI Type

Here, you can select the UI type you want to support in your module:

![abp-studio-create-new-module-dialog-step-ui](images/abp-studio-create-new-module-dialog-step-ui.png)

{{ if UI == "MVC" }}

In this tutorial, we are selecting the MVC UI since we are building that module only for our `ModularCrm` solution and we are using the MVC UI in our application. So, select the MVC UI and click the *Next* button.

{{ else if UI == "NG" }}

In this tutorial, we are selecting the Angular UI since we are building that module only for our `ModularCrm` solution and we are using the Angular UI in our application. So, select the Angular UI and click the *Next* button.

{{ end }}

A module;

* May not contain a UI and leaves the UI development to the final application.
* May contain a single UI implementation that is typically in the same technology as the main application.
* May contain more than one UI implementation if you want to create a reusable application module and you want to make that module usable by different applications with different UI technologies. For example, all of [pre-built ABP modules](https://abp.io/modules) support multiple UI options.

### Selecting the Database Provider

The next step is to select the database provider (or providers) you want to support with your module:

{{ if DB == "EF" }}

![abp-studio-create-new-module-dialog-step-db-ef](images/abp-studio-create-new-module-dialog-step-db-ef.png)

Since our main application is using Entity Framework Core and we will use the `ModularCrm.Products` module only for that main application, we can select the *Entity Framework Core* option and click the *Next* button.

{{ else }}

![abp-studio-create-new-module-dialog-step-db-mongo](images/abp-studio-create-new-module-dialog-step-db-mongo.png)

Since our main application is using MongoDB and we will use the `ModularCrm.Products` module only for that main application, we can select the *MongoDB* option and click the *Next* button.

{{ end }}

![abp-studio-create-new-module-dialog-step-additional-options](images/abp-studio-create-new-module-dialog-step-additional-options.png)

Lastly, you can uncheck the *Include Tests* option if you don't want to include test projects in your module. Click the *Create* button to create the new module.

### Exploring the New Module

After adding the new module, the *Solution Explorer* panel will show the module projects. The exact structure depends on your selected configurations - the following figure shows an example module structure{{ if DB == "EF"}} (for UI: MVC / Razor Pages, DB: Entity Framework Core){{else}} (for UI: Angular, DB: MongoDB){{end}}:

{{ if DB == "EF"}}

![abp-studio-solution-explorer-two-modules-mvc-ef](images/abp-studio-solution-explorer-two-modules-mvc-ef.png)

{{ else }}

![abp-studio-solution-explorer-two-modules-angular-mongo](images/abp-studio-solution-explorer-two-modules-angular-mongo.png)

{{ end }}

The new `ModularCrm.Products` module has been created and added to the solution. The `ModularCrm.Products` module has a separate and independent .NET solution. Right-click the `ModularCrm.Products` module and select the *Open with* -> *Explorer* command:

![abp-studio-open-in-explorer](images/abp-studio-open-in-explorer.png)

This command opens the solution folder in your file system:

{{ if UI != "NG" }}

![product-module-folder-without-angular](images/product-module-folder-without-angular.png)

{{ else }}

![product-module-folder-angular](images/product-module-folder-angular.png)

{{ end }}

You can open `ModularCrm.Products.sln` in your favorite IDE (e.g. Visual Studio):

![product-module-visual-studio](images/product-module-visual-studio.png)

As seen in the preceding figure, the `ModularCrm.Products` solution consists of several layers, each has own responsibility (even though your project structure might slightly differ based on your UI and database choices).

### Installing the Product Module to the Main Application

{{ if UI == "MVC" }}

A module does not contain an executable application inside. The `Modular.Products.Web` project is just a class library project, not an executable web application. A module should be installed in an executable application to run it.

{{ else if == "NG" }}

A module does not contain an executable application inside. The angular project is just a modular project, not an executable web application. A module should be installed in an executable application to run it.

{{ end }}

> **Ensure that the web application is not running in [Solution Runner](../../studio/running-applications.md) or in your IDE. Installing a module to a running application will produce errors.**

The product module has yet to be related to the main application. Right-click on the `ModularCrm` module (inside the `main` folder) and select the *Import Module* command:

![abp-studio-import-module](images/abp-studio-import-module.png)

The *Import Module* command opens a dialog as shown below:

![abp-studio-import-module-dialog](images/abp-studio-import-module-dialog.png)

Select the `ModularCrm.Products` module and check the *Install this module* option. If you don't check that option, it only imports the module but doesn't set project dependencies. Importing a module without installation can be used to set up your project dependencies manually. We want to make it automatically, so check the *Install this module* option.

When you click the *OK* button, ABP Studio opens the *Install Module* dialog:

{{ if DB == "EF" }}

![abp-studio-module-installation-dialog-ef](images/abp-studio-module-installation-dialog-ef.png)

{{ else }}

![abp-studio-module-installation-dialog-mongo](images/abp-studio-module-installation-dialog-mongo.png)

{{ end }}

This dialog simplifies installing a multi-layer module to a single-layer application. It automatically determines which package of the `ModularCrm.Products` module should be installed to which package of the main application.

The default package match is good for this tutorial, so you can click the *OK* button to proceed.

### Building the Main Application

After the installation, build the entire solution by right-clicking on the `ModularCrm` module (under the `main` folder) and selecting the *Dotnet CLI* -> *Graph Build* command:

![abp-studio-graph-build](images/abp-studio-graph-build.png)

Graph Build is a dotnet CLI command that recursively builds all the referenced dotnet projects, even if they are not part of the root solution.

> While developing multi-module solutions with ABP Studio, you may need to perform *Graph Build* on the root/main module if you change the depending modules.

### Run the Main Application

{{ if UI == "MVC" }}

Open the *Solution Runner* panel, click the *Play* button (near to the solution root), right-click the `ModularCrm` application and select the *Browse* command. It will open the web application in the built-in browser. Then you can navigate to the *Products* page on the main menu of the application to see the Products page that is coming from the `ModularCrm.Products` module:

![abp-studio-solution-runner-initial-product-page-mvc](images/abp-studio-solution-runner-initial-product-page-mvc.png)

{{ else if == "NG" }}

Before running the main application, we should reference our angular module in the main angular application. To do that, open the `angular` folder in your root directory in an IDE, and make the following changes:

1. Open the `tsconfig.json` file and update the _paths_ section as follows:

```json
{
    //...

    "paths": {
      "@proxy": [
        "src/app/proxy/index.ts"
      ],
      "@proxy/*": [
        "src/app/proxy/*"
      ],

      //add the following lines 👇
      "@angular/*": ["node_modules/@angular/*"],
      "@abp/*":["node_modules/@abp/*"],
      "@volo/*": ["node_modules/@volo/*"],
      "@volosoft/*": ["node_modules/@volosoft/*"],

      "@modularcrm/products": [
        "../modules/modularcrm.products/angular/projects/products/src/public-api.ts"
      ],
      "@modularcrm/products/config": [
        "../modules/modularcrm.products/angular/projects/products/config/src/public-api.ts"
      ],
    },

    //...
}
```

2. Then, open the `app.module.ts` file and import the `ProductsConfigModule`:

```diff
//other import statements...
+ import { ProductsConfigModule } from '@modularcrm/products/config';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    AppRoutingModule,
    ThemeSharedModule,
    CoreModule,
    ThemeLeptonXModule.forRoot(),
    SideMenuLayoutModule.forRoot(),
+   ProductsConfigModule.forRoot()
  ],
  providers: [APP_ROUTE_PROVIDER,
    provideAbpCore(
      withOptions({
        environment,
        registerLocaleFn: registerLocale(),
      }),
    ),
    provideAbpOAuth(),
    provideIdentityConfig(),
    provideSettingManagementConfig(),
    provideFeatureManagementConfig(),
    provideAccountConfig(),
    provideTenantManagementConfig(),
    provideAbpThemeShared(),
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}

```

3. After importing the related module, now we can add its routing info to the `app-routing.module.ts` file:

```ts
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProductsModule } from '@modularcrm/products'; //importing the ProductsModule

const routes: Routes = [
  //other routings...

  {
    path: 'products',
    loadChildren: () => import('@modularcrm/products').then(m => m.ProductsModule),
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {})],
  exports: [RouterModule],
})
export class AppRoutingModule {}

```

4. Finally, you should run the `npx yarn install` (or `yarn install`, if _yarn_ is globally installed) command under the **modules/modularcrm.products/angular** directory:

![product-module-yarn-install-ng](images/product-module-yarn-install-ng.png)

After these configurations, you can open the *Solution Runner* panel, click the *Play* button (near to the solution root) to run the backend and the angular applications. After the applications are up & running, right-click the `ModularCrm.Angular` application and select the *Browse* command. It will open the angular application in the built-in browser. Then you can navigate to the _Products_ page on the main menu of the application to see the Products page that is coming from the `ModularCrm.Products` module:

![abp-studio-solution-runner-initial-product-page-ng](images/abp-studio-solution-runner-initial-product-page-ng.png)

{{ else }}

//TODO: blazor-ui...

{{ end }}

## Summary

In this part, we've created a new module to manage products in our modular application. Then we installed the new module to the main application and run the solution to test if it has successfully installed.

In the next part, you will learn how to create entities, services and a basic user interface for the products module.
