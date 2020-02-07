
!!!!!Important, i primaryly use MSSQL as database choice, this script was created by my automated tool that can be found in my github page and is meant as guidence for future port to mysql as a main database in rokono control
having said this this database is the same as the database provided in the MSSQLDatabaseGenerationScript.sql  however i am not sure how much its optimized for production!!! 

If you decide to use this database make sure that you add the nuget package in entity framewrok that supports mysql and regenerate the databse context!

CREATE TABLE IF NOT EXISTS AssociatedProjectBoards (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  BoardId INT,
  ProjectId INT,
  Position INT
);CREATE TABLE IF NOT EXISTS Risks (Id INT NOT NULL);CREATE TABLE IF NOT EXISTS Efforts (Id INT NOT NULL);CREATE TABLE IF NOT EXISTS ValueAreas (Id INT NOT NULL);CREATE TABLE IF NOT EXISTS AssociatedWrorkItemChildren (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  WorkItemId INT,
  WorkItemChildId INT,
  RelationType INT
);CREATE TABLE IF NOT EXISTS Commits (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  DateOfCommit DATETIME
);CREATE TABLE IF NOT EXISTS Branches (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  ProjectId INT NOT NULL,
  DateOfCreation DATETIME
);CREATE TABLE IF NOT EXISTS AssociatedBranchCommits (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  CommitId INT NOT NULL,
  BranchId INT NOT NULL
);CREATE TABLE IF NOT EXISTS WorkItemRealtionshipType (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS UserAccounts (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  CreationDate DATETIME NOT NULL,
  ProjectRights INT
);CREATE TABLE IF NOT EXISTS AssociatedRepositoryBranches (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  RepositoryId INT NOT NULL,
  BranchId INT NOT NULL
);CREATE TABLE IF NOT EXISTS WorkItemTypes (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS WorkItem (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  WorkItemTypeId INT,
  AssignedAccount INT,
  StateId INT,
  AreaId INT,
  StartDate DATETIME,
  EndDate DATETIME,
  PriorityId INT,
  ClassificationId INT,
  DevelopmentId INT,
  ParentId INT,
  Reason INT,
  Iteration INT,
  itemPriority INT,
  Severity INT,
  Activity INT,
  BranchId INT,
  FoundInBuild INT,
  IntegratedInBuild INT,
  ReasonId INT,
  RelationId INT,
  RiskId INT,
  ValueAreaId INT,
  DueDate DATETIME
);CREATE TABLE IF NOT EXISTS Projects (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  RepositoryId INT NOT NULL,
  CreationDate DATETIME,
  BoardId INT
);CREATE TABLE IF NOT EXISTS Repository (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS Files (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  DateOfFile DATETIME
);CREATE TABLE IF NOT EXISTS AssociatedCommitFiles (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  FileId INT,
  CommitId INT,
  DateOfCommit DATETIME
);CREATE TABLE IF NOT EXISTS Boards (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  RepositoryId INT NOT NULL,
  BoardType INT NOT NULL
);CREATE TABLE IF NOT EXISTS AssociatedBoardWorkItems (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  BoardId INT NOT NULL,
  WorkItemId INT NOT NULL,
  ProjectId INT
);CREATE TABLE IF NOT EXISTS AssociatedProjectMembers (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  UserAccountId INT NOT NULL,
  ProjectId INT NOT NULL,
  RepositoryId INT NOT NULL,
  CanCommit INT NOT NULL,
  CanClone INT NOT NULL,
  CanViewWork INT NOT NULL,
  CanCreateWork INT NOT NULL,
  CanDeleteWork INT NOT NULL
);CREATE TABLE IF NOT EXISTS WorkItemStates (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS WorkItemAreas (ID INT NOT NULL);CREATE TABLE IF NOT EXISTS WorkItemPriorities (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS WorkItemSeverities (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS WorkItemActivity (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS WorkItemIterations (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS WorkItemReasons (Id INT AUTO_INCREMENT PRIMARY KEY);CREATE TABLE IF NOT EXISTS Builds (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  FrameworkVersion INT,
  DateOfBuild DATETIME,
  CompleationStatus INT,
  AccountId INT,
  PlatformId INT
);CREATE TABLE IF NOT EXISTS AssociatedProjectBuilds (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  RepositoryId INT,
  BuildId INT,
  ProjectId INT
);CREATE TABLE IF NOT EXISTS WorkItemRelations (Id INT AUTO_INCREMENT PRIMARY KEY);
ALTER TABLE
  AssociatedBoardWorkItems
ADD
  FOREIGN KEY (BoardId) REFERENCES Boards(Id);
ALTER TABLE
  AssociatedBoardWorkItems
ADD
  FOREIGN KEY (WorkItemId) REFERENCES WorkItem(Id);
ALTER TABLE
  AssociatedBoardWorkItems
ADD
  FOREIGN KEY (ProjectId) REFERENCES Projects(Id);
ALTER TABLE
  AssociatedBranchCommits
ADD
  FOREIGN KEY (CommitId) REFERENCES Commits(Id);
ALTER TABLE
  AssociatedBranchCommits
ADD
  FOREIGN KEY (BranchId) REFERENCES Branches(Id);
ALTER TABLE
  AssociatedCommitFiles
ADD
  FOREIGN KEY (FileId) REFERENCES Files(Id);
ALTER TABLE
  AssociatedCommitFiles
ADD
  FOREIGN KEY (CommitId) REFERENCES Commits(Id);
ALTER TABLE
  AssociatedProjectBoards
ADD
  FOREIGN KEY (BoardId) REFERENCES Boards(Id);
ALTER TABLE
  AssociatedProjectBoards
ADD
  FOREIGN KEY (ProjectId) REFERENCES Projects(Id);
ALTER TABLE
  AssociatedProjectBuilds
ADD
  FOREIGN KEY (RepositoryId) REFERENCES Repository(Id);
ALTER TABLE
  AssociatedProjectBuilds
ADD
  FOREIGN KEY (BuildId) REFERENCES Builds(Id);
ALTER TABLE
  AssociatedProjectBuilds
ADD
  FOREIGN KEY (ProjectId) REFERENCES Projects(Id);
ALTER TABLE
  AssociatedProjectMembers
ADD
  FOREIGN KEY (UserAccountId) REFERENCES UserAccounts(Id);
ALTER TABLE
  AssociatedProjectMembers
ADD
  FOREIGN KEY (ProjectId) REFERENCES Projects(Id);
ALTER TABLE
  AssociatedProjectMembers
ADD
  FOREIGN KEY (RepositoryId) REFERENCES Repository(Id);
ALTER TABLE
  AssociatedRepositoryBranches
ADD
  FOREIGN KEY (RepositoryId) REFERENCES Repository(Id);
ALTER TABLE
  AssociatedRepositoryBranches
ADD
  FOREIGN KEY (BranchId) REFERENCES Branches(Id);
ALTER TABLE
  AssociatedWrorkItemChildren
ADD
  FOREIGN KEY (WorkItemId) REFERENCES WorkItem(Id);
ALTER TABLE
  AssociatedWrorkItemChildren
ADD
  FOREIGN KEY (WorkItemChildId) REFERENCES WorkItem(Id);
ALTER TABLE
  AssociatedWrorkItemChildren
ADD
  FOREIGN KEY (RelationType) REFERENCES WorkItemTypes(Id);
ALTER TABLE
  Branches
ADD
  FOREIGN KEY (ProjectId) REFERENCES Projects(Id);
ALTER TABLE
  Projects
ADD
  FOREIGN KEY (RepositoryId) REFERENCES Repository(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (WorkItemTypeId) REFERENCES WorkItemTypes(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (AssignedAccount) REFERENCES UserAccounts(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (StateId) REFERENCES WorkItemStates(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (AreaId) REFERENCES WorkItemAreas(ID);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (PriorityId) REFERENCES WorkItemPriorities(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (Iteration) REFERENCES WorkItemIterations(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (Severity) REFERENCES WorkItemSeverities(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (Activity) REFERENCES WorkItemActivity(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (ReasonId) REFERENCES WorkItemReasons(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (RelationId) REFERENCES WorkItemRelations(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (RiskId) REFERENCES Risks(Id);
ALTER TABLE
  WorkItem
ADD
  FOREIGN KEY (ValueAreaId) REFERENCES ValueAreas(Id);
