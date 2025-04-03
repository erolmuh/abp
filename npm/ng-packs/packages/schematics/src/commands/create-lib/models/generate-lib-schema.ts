export interface GenerateLibSchema {
  /**
   * Angular package name will create
   */
  packageName: string;

  /**
   * İs the package a library or a library module
   */
  isSecondaryEntrypoint: boolean;
  /**
   * İs the package has standalone template
   */
  isStandaloneTemplate: boolean;

  isModuleTemplate: boolean;

  override: boolean;

  target: string;
}
