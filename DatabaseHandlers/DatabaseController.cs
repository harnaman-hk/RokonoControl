namespace Rokono_Control.DatabaseHandlers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Rokono_Control.DataHandlers;
    using Rokono_Control.Models;
    using RokonoControl.DatabaseHandlers.WorkItemHandlers;
    using RokonoControl.Models;

    public class DatabaseController : IDisposable
    {
        RokonoControlContext Context;
        private int I{get;set;}
        private int InternalId {get;set;}
        internal List<Repository> GetAllRepositories()
        {
            return Context.Repository.Include(x=>x.Projects).ToList();
        }

        internal List<BindingCards> GetProjectCards(int projectId, int workItemType)
        {
            var boards = Context.AssociatedProjectBoards.Include(x=>x.Board)
                                                .ThenInclude(Board => Board.AssociatedBoardWorkItems)
                                                .ThenInclude(AssociatedBoardWorkItems => AssociatedBoardWorkItems.WorkItem)    
                                                .ThenInclude(WorkItem => WorkItem.State)   
                                                .Where(x=>x.ProjectId == projectId && x.Board.AssociatedBoardWorkItems.Any(z=>z.WorkItem.WorkItemTypeId == workItemType)).ToList();
            
            var result = new List<BindingBoard>();
            var Cards = new List<BindingCards>();
            boards.ForEach(x=>{
                x.Board.AssociatedBoardWorkItems.Where(y=>y.WorkItem.WorkItemTypeId == workItemType).ToList().ForEach(y=>{
                    var related = new List<RelatedItems>();
                    y.WorkItem.AssociatedWorkItemDuplicatesWorkItem.ToList().ForEach(z=>{
                        related.Add(new RelatedItems{
                            Id = z.WorkItem.Id,
                            Name = z.WorkItem.Title
                        });
                    });
                    y.WorkItem.AssociatedWorkItemPredecessorsWorkItem.ToList().ForEach(z=>{
                        related.Add(new RelatedItems{
                            Id = z.WorkItem.Id,
                            Name = z.WorkItem.Title
                        });
                    });
                    y.WorkItem.AssociatedWorkItemRelatedWorkItem.ToList().ForEach(z=>{
                        related.Add(new RelatedItems{
                            Id = z.WorkItem.Id,
                            Name = z.WorkItem.Title
                        });
                    });
                    y.WorkItem.AssociatedWorkItemSuccessorsWorkItem.ToList().ForEach(z=>{
                        related.Add(new RelatedItems{
                            Id = z.WorkItem.Id,
                            Name = z.WorkItem.Title
                        });
                    });
                    y.WorkItem.AssociatedWorkItemTestsWorkItem.ToList().ForEach(z=>{
                        related.Add(new RelatedItems{
                            Id = z.WorkItem.Id,
                            Name = z.WorkItem.Title

                        });
                    });
                    Cards.Add(new BindingCards{
                        Id = y.WorkItem.Id,
                        Summary = y.WorkItem.Title,
                        Status = x.Board.BoardName,
                       // Children = related
                    });
                });
            });
            return Cards;
        }

        internal object GetAllWorkItemTypes() => Context.WorkItemTypes.ToList();

        internal object GetNewNotifications(object value)
        {
            throw new NotImplementedException();
        }

        internal WorkItem GetWorkItem(int workItem, int projectId)
        {
             return Context.AssociatedBoardWorkItems
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.WorkItemType)
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemDuplicatesWorkItem)
                        .ThenInclude(AssociatedWorkItemDuplicatesWorkItem => AssociatedWorkItemDuplicatesWorkItem.WorkItemChild)
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemPredecessorsWorkItem)
                        .ThenInclude(AssociatedWorkItemPredecessorsWorkItem => AssociatedWorkItemPredecessorsWorkItem.WorkItemChild)
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemRelatedWorkItem)
                        .ThenInclude(AssociatedWorkItemRelatedWorkItem => AssociatedWorkItemRelatedWorkItem.WorkItemChild)
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemSuccessorsWorkItem)
                        .ThenInclude(AssociatedWorkItemSuccessorsWorkItem => AssociatedWorkItemSuccessorsWorkItem.WorkItemChild)
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemTestsWorkItem)
                        .ThenInclude(AssociatedWorkItemTestsWorkItem => AssociatedWorkItemTestsWorkItem.WorkItemChild)
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem=> WorkItem.AssignedAccountNavigation)
                        .FirstOrDefault(x=>x.ProjectId == projectId && x.WorkItemId == workItem).WorkItem;
        }

        internal void ChangeWorkItemBoard(IncomingCardRequest card)
        {
            var newBoardAssociation = Context.AssociatedProjectBoards.Include(x=>x.Board).FirstOrDefault(x=> x.ProjectId == card.ProjectId 
                                                                                    && x.Board.BoardName == card.Board);
            var currentAssociation = Context.AssociatedBoardWorkItems.FirstOrDefault(x=> x.WorkItemId == card.CardId);

            currentAssociation.BoardId = newBoardAssociation.Board.Id;
            Context.Attach(currentAssociation);
            Context.Entry(currentAssociation).Property("BoardId").IsModified = true;
            Context.SaveChanges();


        }

        internal List<Branches> GetProjectBranches(int projectId)
        {
            var branches = Context.Projects.Include(x=>x.Repository)
                                   .ThenInclude(Repository => Repository.AssociatedRepositoryBranches)
                                   .ThenInclude(AssociatedRepositoryBranches => AssociatedRepositoryBranches.Branch)
                                   .FirstOrDefault(x=>x.Id == projectId);
            if(branches != null)
                return branches.Repository.AssociatedRepositoryBranches.Select(y=>y.Branch).ToList();
            else
                return null;
                                   
        }

        internal OutgoingBoundRelations GetAllWorkItemRelations(int workItemId, int projectId)
        {
            var workItem = Context.WorkItem.FirstOrDefault(x=>x.Id == workItemId);
            var relations = new List<BindingWorkItemRelation>();
            var bindingRelations = new List<BindingWorkItemRelation>();
            var successorsDb = Context.WorkItem.Include(x=>x.AssociatedWorkItemSuccessorsWorkItem)
                                            .ThenInclude(AssociatedWorkItemSuccessorsWorkItem => AssociatedWorkItemSuccessorsWorkItem.WorkItemChild)
                                            .FirstOrDefault(x=>x.Id == workItemId);
            if(successorsDb != null)
                bindingRelations.AddRange(successorsDb.AssociatedWorkItemSuccessorsWorkItem 
                                             .Select(y=> new BindingWorkItemRelation{
                                                WorkItem = new BindingWorkItemDTO
                                                {
                                                   Title = y.WorkItemChild.Title,
                                                   Id = y.WorkItemChild.Id
                                                },
                                                RelationType = "Successors"
                                            }).ToList());
             var testsDb = Context.WorkItem.Include(x=>x.AssociatedWorkItemTestsWorkItem)
                                            .ThenInclude(AssociatedWorkItemTestsWorkItem => AssociatedWorkItemTestsWorkItem.WorkItemChild)
                                            .FirstOrDefault(x=>x.Id == workItemId);
                                           
            if(testsDb != null)
                bindingRelations.AddRange(testsDb.AssociatedWorkItemTestsWorkItem
                                            .Select(y=> new BindingWorkItemRelation{
                                                WorkItem = new BindingWorkItemDTO
                                                {
                                                   Title = y.WorkItemChild.Title,
                                                   Id = y.WorkItemChild.Id
                                                },
                                                RelationType = "Tests"
                                            }).ToList());
            var predeccessor = Context.WorkItem.Include(x=>x.AssociatedWorkItemPredecessorsWorkItem)
                                            .ThenInclude(AssociatedWorkItemPredecessorsWorkItem => AssociatedWorkItemPredecessorsWorkItem.WorkItemChild)
                                            .FirstOrDefault(x=>x.Id == workItemId);
            if(predeccessor != null)
                bindingRelations.AddRange(predeccessor.AssociatedWorkItemPredecessorsWorkItem
                                            .Select(y=> new BindingWorkItemRelation{
                                                WorkItem = new BindingWorkItemDTO
                                                {
                                                   Title = y.WorkItemChild.Title,
                                                   Id = y.WorkItemChild.Id
                                                },
                                                RelationType = "Predeccessor"
                                            }).ToList());
                                        
            var duplicates = Context.WorkItem.Include(x=>x.AssociatedWorkItemDuplicatesWorkItem)
                                            .ThenInclude(AssociatedWorkItemDuplicatesWorkItem => AssociatedWorkItemDuplicatesWorkItem.WorkItemChild)
                                            .FirstOrDefault(x=>x.Id == workItemId);
                                  
            if(duplicates != null)
                bindingRelations.AddRange(duplicates.AssociatedWorkItemDuplicatesWorkItem
                                            .Select(y=> new BindingWorkItemRelation{
                                                WorkItem = new BindingWorkItemDTO
                                                {
                                                   Title = y.WorkItemChild.Title,
                                                   Id = y.WorkItemChild.Id
                                                },
                                                RelationType = "Duplicates"
                                            }).ToList());
             var related = Context.WorkItem.Include(x=>x.AssociatedWorkItemRelatedWorkItem)
                                            .ThenInclude(AssociatedWorkItemRelatedWorkItem => AssociatedWorkItemRelatedWorkItem.WorkItemChild)
                                            .FirstOrDefault(x=>x.Id == workItemId);
            if(related != null)
                bindingRelations.AddRange(related.AssociatedWorkItemRelatedWorkItem
                                            .Select(y=> new BindingWorkItemRelation{
                                                WorkItem = new BindingWorkItemDTO
                                                {
                                                   Title = y.WorkItemChild.Title,
                                                   Id = y.WorkItemChild.Id
                                                },
                                                RelationType = "Related"
                                            }).ToList());
            var res = new StringBuilder();

            relations.AddRange(bindingRelations);
            res.AppendLine ($"class {RemoveWhitespace(workItem.Title)}");
            res.AppendLine("{"); 
            res.AppendLine("}");
            relations.ForEach(x=>{
                res.AppendLine($"class {RemoveWhitespace(x.WorkItem.Title)}");
                res.AppendLine("{");
                res.AppendLine($" is {x.RelationType} of {workItem.Title}");
                res.AppendLine($" Open Work Item [[[https://localhost:5001/Dashboard/EditWorkItem?projectId={projectId}&&workItem={x.WorkItem.Id}]]]");
                res.AppendLine("}");
            });
        //    AssociatedBoardWorkItems "1" *-- "many" Boards
            relations.ForEach(x=>{
                res.AppendLine($" {RemoveWhitespace(x.WorkItem.Title)} \"1\" *--  \"{x.RelationType}\" {RemoveWhitespace(workItem.Title)} ");
            });
            return new OutgoingBoundRelations
            {
              WorkItems = relations,
              UmlData = res.ToString()
            };
        }

        internal void AssociatedRelation(IncomignWorkItemRelation incomingRelation)
        {
            var currentWorkItem = Context.WorkItem.FirstOrDefault(x=>x.Id == incomingRelation.CurrWorkItemId);
             switch(currentWorkItem.WorkItemTypeId)
            {
                
                case 1:
                 Context.AssociatedWrorkItemChildren.Add(new AssociatedWrorkItemChildren{
                    WorkItemChildId = currentWorkItem.Id,
                    WorkItemId = incomingRelation.WorkItemId,
                });
                Context.SaveChanges();
                break;
                case 2:
                Context.AssociatedWrorkItemParents.Add(new AssociatedWrorkItemParents{
                    WorkItemChildId = currentWorkItem.Id,
                    WorkItemId = incomingRelation.WorkItemId,
                });
                Context.SaveChanges();
                break;
                case 3:
                Context.AssociatedWorkItemDuplicates.Add(new AssociatedWorkItemDuplicates{
                    WorkItemChildId = currentWorkItem.Id,
                    WorkItemId = incomingRelation.WorkItemId,
                });
                Context.SaveChanges();
                break;
                case 4:
                Context.AssociatedWorkItemSuccessors.Add(new AssociatedWorkItemSuccessors{
                    WorkItemChildId = currentWorkItem.Id,
                    WorkItemId = incomingRelation.WorkItemId,
                });
                Context.SaveChanges();
                break;
                case 5:
                 Context.AssociatedWorkItemTests.Add(new AssociatedWorkItemTests{
                    WorkItemChildId = currentWorkItem.Id,
                    WorkItemId = incomingRelation.WorkItemId,
                });
                Context.SaveChanges();
                break;
                case 6:
                 Context.AssociatedWorkItemPredecessors.Add(new AssociatedWorkItemPredecessors{
                    WorkItemChildId = currentWorkItem.Id,
                    WorkItemId = incomingRelation.WorkItemId,
                });
                Context.SaveChanges();
                break;
            }
        }

        public string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        internal OutgoingSourceFile GetSelectedFileByName(string fileName, int projectId,int branchId)
        {
            var project = Context.Projects.Include(x=>x.Repository).FirstOrDefault(x=>x.Id == projectId);
            var branch = Context.Branches.FirstOrDefault(x=>x.Id == branchId);
            var fileLanauge = GitRepositoryManager.GetFileLanaguage(fileName);
            var branchCommits = Execute($"{Program.Configuration.ShellScripts.FirstOrDefault(x=>x.Name == "GetGitList.sh").Path}",$"{project.Repository.FolderPath} {branch.BranchName}");
            var fileData =ExecuteSingle($"{Program.Configuration.ShellScripts.FirstOrDefault(x=>x.Name == "GetCommitFile.sh").Path}", $"{project.Repository.FolderPath} {branchCommits.FirstOrDefault()}:{fileName}");
             return new OutgoingSourceFile{ Data = fileData, LanguageType=  fileLanauge};
        }

        internal List<OutgoingBindingWorkItem> GetAllWorkItems(int projectId)
        {
            return Context.AssociatedBoardWorkItems
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.WorkItemType)
                        .Include(x=>x.WorkItem)
                        .ThenInclude(WorkItem => WorkItem.State)
                        .Where(x=>x.ProjectId == projectId)
                        .Select(x=> new OutgoingBindingWorkItem{
                            Id = x.WorkItem.Id,
                            Title = x.WorkItem.Title,
                            ItemState = x.WorkItem.State.StateName,
                            ItemStateId = x.WorkItem.State.Id,
                            ItemType = x.WorkItem.WorkItemType.TypeName,
                            ItemTypeId = x.WorkItem.WorkItemType.Id
                        })
                        .ToList();
        }

        internal List<string> GetBranchFiles(int projectId, int branchId)
        {   
            var result = new List<string>();
            var project = Context.Projects.FirstOrDefault(x=>x.Id == projectId);
            if(project == null)
                return null;
            var getBranch = Context.AssociatedRepositoryBranches.Include(x=>x.Branch)
                                                                .Include(x=>x.Repository)
                                                                .FirstOrDefault(x=> x.RepositoryId == project.RepositoryId && x.Branch.Id == branchId);
            if(getBranch != null)
            {
                var name = getBranch.Repository.FolderPath;
                // var fileName = file.FilePath.Split('/').LastOrDefault();
                //var fileLanauge = GitRepositoryManager.GetFileLanaguage(fileName);
                result =Execute($"{Program.Configuration.ShellScripts.FirstOrDefault(x=>x.Name == "LsFiles.sh").Path}", $"{name}");
                
            }
            return result;
        }



        internal List<CommitFileHirarhicalData> GetFileHirarchy(List<string> files)
        {
            var folders = new List<CommitFileHirarhicalData>();
            var data = new List<CommitFileHirarhicalData>();
            var count = 1;
            InternalId = 1;
            files.ForEach(x=>{

                   // folders.Add(GenerateDirectory(x,folders.Count));
                   var item = GenerateDirectory(x,$"{count++}");
                   if(item != null)
                    folders.Add(item);
                   count++;
            });
            data.AddRange(folders);
            return data;
        }
        public CommitFileHirarhicalData GenerateDirectory(string path, string count)
        {
              if(Directory.Exists(path))
               {
                   var dFiles = Directory.EnumerateFiles(path);
                   var item = new CommitFileHirarhicalData{
                       Name = path.Split("/").LastOrDefault(),
                       FullPathName = path,
                        InternalId = InternalId,
                       Id = $"{count+1}",
                       SubChild = new List<CommitFileHirarhicalData>()
                   };
                   item.SubChild = GenerateSubDirectory(item,dFiles.ToList(),$"{count}-{I++}",path);
                    InternalId++;

                   var directories = Directory.GetDirectories(path);
                    directories.ToList().ForEach(e=>{
                        item.SubChild.Add(GenerateDirectory(e,$"{count}-{item.SubChild.Count+ 1}"));
                    });

                    return item;
               }
               else
               {
                   var item = new CommitFileHirarhicalData{
                       Name = path,
                        InternalId = InternalId,

                       FullPathName = path,
                       Id = $"{count+1}"
                   };
                    InternalId++;

                   return item;
               }
                
        }
 

        public List<CommitFileHirarhicalData> GenerateSubDirectory(CommitFileHirarhicalData item, List<string> files, string parent,string directory)
        {
            InternalId++;

             files.ToList().ForEach(z=>{
                item.SubChild.Add(new CommitFileHirarhicalData{
                    Name = z.Split("/").LastOrDefault(),
                    FullPathName = z,
                    Id = parent,
                    InternalId = InternalId,

                    SubChild = new List<CommitFileHirarhicalData>()
                });
            });
            
            return item.SubChild;
        }

        public DatabaseController()
        {
            Context = new RokonoControlContext();
        }

        internal List<Branches> GetBranchesForProject(int projectId)
        {
            return Context.AssociatedRepositoryBranches
                        .Include(x=>x.Branch)
                        .Include(x=>x.Repository)
                        .ThenInclude(Repository => Repository.Projects)
                        .Where(x=>x.Repository.Projects.Any(y=>y.Id == projectId))
                        .Select(x=>new Branches{
                            BranchName = x.Branch.BranchName,
                            Id = x.Branch.Id,
                        }).ToList();

        }

        internal UserAccounts LoginUser(IncomingLoginRequest request)
        {
            var account = Context.UserAccounts
                                 .FirstOrDefault(x => x.Email == request.Email 
                                 && x.Password == request.Password);
            if (account != null)
                return account;
            else
                return null;
        }

        internal List<CommitFileHirarhicalData> GetCommitFilesHirarchy(string commitId)
        {
            var folders = new List<CommitFileHirarhicalData>();
            var data = new List<CommitFileHirarhicalData>();
            var files = Context.AssociatedCommitFiles
                    .Include(x=>x.File)
                    .Include(x=>x.Commit)
                    .Where(x=>x.Commit.CommitKey == commitId)
                    .ToList();

            files.ForEach(x=>{
                var ifSingle = x.File.FilePath.Split('/').ToList();
                if(ifSingle.Count() <= 1)
                    data.Add(new CommitFileHirarhicalData{
                        Id = $"{data.Count+1}",
                        Name = x.File.FilePath,
                        ItemId = x.File.Id

                    });
                        
            });
            var count = data.Count + 1;
            files.ForEach(x=>{
                    var ifSingle = x.File.FilePath.Split('/').ToList();
                    var file = ifSingle.LastOrDefault();
                
                    if(ifSingle.Count() > 1)
                    {
                        ifSingle.Remove(file);
                        var folder = string.Empty;
                        ifSingle.ForEach(cFolder =>{
                                folder += $"{cFolder}/";
                        });
                        if(folders.FirstOrDefault(cFolders => cFolders.Name == folder) == null)
                            folders.Add(new CommitFileHirarhicalData{
                                Id = $"{count++}",
                                Name = folder,
                                SubChild = new List<CommitFileHirarhicalData>(),
                                ItemId = x.File.Id

                            });
                        else
                        {
                            if( folders.FirstOrDefault(z => z.Name == folder).SubChild == null)
                                folders.FirstOrDefault(z => z.Name == folder).SubChild = new List<CommitFileHirarhicalData>();
                            folders.FirstOrDefault(z => z.Name == folder).SubChild.Add(new CommitFileHirarhicalData{
                                Id = $"{count}-{folders.FirstOrDefault(z => z.Name == folder).SubChild.Count + 1}",
                                Name = file,
                                ItemId = x.File.Id
                            });
                        }
                    }
            });
            data.AddRange(folders);
            return data;
         }

        internal OutgoingSourceFile GetSelectedFileById(int fileId, int branch)
        {
            var file = Context.Files
                            .Include(x=>x.AssociatedCommitFiles)
                            .ThenInclude(AssociatedCommitFiles => AssociatedCommitFiles.Commit)
                            .FirstOrDefault(x=>x.Id == fileId);
            var commitKey = file.AssociatedCommitFiles.FirstOrDefault().Commit.CommitKey;
            var repository = Context.Repository
                                    .Include(x=>x.AssociatedRepositoryBranches)
                                    .ThenInclude(AssociatedRepositoryBranches => AssociatedRepositoryBranches.Branch)
                                    .ThenInclude(Branch => Branch.AssociatedBranchCommits)
                                    .ThenInclude(AssociatedBranchCommits => AssociatedBranchCommits.Commit)
                                    .FirstOrDefault(x=>
                                    x.AssociatedRepositoryBranches
                                    .Any(z=>
                                    z.Branch.AssociatedBranchCommits
                                    .Any(y=>
                                    y.Commit.CommitKey == commitKey)));
            var fileName = file.FilePath.Split('/').LastOrDefault();
            var fileLanauge = GitRepositoryManager.GetFileLanaguage(fileName);
            var fileData =ExecuteSingle($"{Program.Configuration.ShellScripts.FirstOrDefault(x=>x.Name == "GetCommitFile.sh").Path}", $"{repository.FolderPath} {commitKey}:{file.FilePath}");
            return new OutgoingSourceFile{ Data = fileData, LanguageType=  fileLanauge};
        }

        internal List<OutgoingCommitData> GetCommitData(int projectId, int branch)
        {
            return  Context.Commits
                        .Include(x=>x.AssociatedBranchCommits)
                        .ThenInclude(AssociatedBranchCommits => AssociatedBranchCommits.Branch)
                        .ThenInclude(Branch => Branch.AssociatedRepositoryBranches)
                        .ThenInclude(AssociatedRepositoryBranches => AssociatedRepositoryBranches.Repository)
                        .ThenInclude(Repository => Repository.Projects)
                        .Where(x=>
                        x.AssociatedBranchCommits 
                        .Any(y=>
                        y.BranchId == branch 
                        && y.Branch.AssociatedRepositoryBranches
                        .Any(z=>
                            z.Repository.Projects
                            .Any(d=>
                                d.Id == projectId)))).Select(commit => new OutgoingCommitData{
                                    Author = commit.CommitedBy,
                                    CommitKey = commit.CommitKey,
                                    Date = commit.DateOfCommit.Value,
                                    PullRequest = "",
                                    Message = commit.CommitData,

                                }).ToList();
         
        }

        internal List<OutgoingCommitData> GetCommitDataMaster(int projectId)
        {
            return  Context.Commits
                        .Include(x=>x.AssociatedBranchCommits)
                        .ThenInclude(AssociatedBranchCommits => AssociatedBranchCommits.Branch)
                        .ThenInclude(Branch => Branch.AssociatedRepositoryBranches)
                        .ThenInclude(AssociatedRepositoryBranches => AssociatedRepositoryBranches.Repository)
                        .ThenInclude(Repository => Repository.Projects)
                        .Where(x=>
                        x.AssociatedBranchCommits 
                        .Any(y=>
                        y.Branch.BranchName == "Master" 
                        && y.Branch.AssociatedRepositoryBranches
                        .Any(z=>
                            z.Repository.Projects
                            .Any(d=>
                                d.Id == projectId)))).Select(commit => new OutgoingCommitData{
                                    Author = commit.CommitedBy,
                                    CommitKey = commit.CommitKey,
                                    Date = commit.DateOfCommit.Value,
                                    PullRequest = "",
                                    Message = commit.CommitData,

                                }).ToList();
         
        }

        internal Branches CreateBrach(string b, int repoId,int projectId)
        {
            var branch = Context.Branches.Add(new  Branches{
                BranchName =b,
                ProjectId = projectId,
            });
            Context.SaveChanges();
            Context.AssociatedRepositoryBranches.Add(new AssociatedRepositoryBranches{
                RepositoryId = repoId,
                BranchId = branch.Entity.Id
            });
            Context.SaveChanges();
            return branch.Entity;

        }

        internal List<string> GetBranches(int repoId)
        {
            return Context.AssociatedRepositoryBranches
                          .Include(x=>x.Branch)
                          .Where(x=>x.RepositoryId == repoId)
                          .Select(x=>x.Branch.BranchName)
                          .ToList();
        }

        internal bool CheckIfBranchCommitExists(string commitId, int branchId)
        {
            return Context.Commits
                          .Include(x=>x.AssociatedBranchCommits)
                          .Where(x=>x.AssociatedBranchCommits.Any(y=>y.BranchId == branchId))
                          .Any(x=>x.CommitKey == commitId);
        }

        internal List<Repository> GetRepositories()
        {
            return Context.Repository.ToList();
        }

        internal List<OutgoingUserAccounts> GetUserAccounts()
        {
            return Context.UserAccounts.Select(x=> new OutgoingUserAccounts{
                Name = $"{x.FirstName.FirstOrDefault()} {x.LastName}",
                AccountId = x.Id
            }).ToList();
        }

        internal  void AssociatedCommitsWithBranch(List<Files> commitFiles, int id,string commitKey, string commitedBy)
        {
            var commit = default(Commits);
            var commiteExists = Context.Commits.Any(x=>x.CommitKey == commitKey);
            if(!commiteExists)
            {
                commit = Context.Commits.Add(new Commits{
                    CommitedBy = commitedBy,
                    CommitKey = commitKey,
                    DateOfCommit = DateTime.Now,
                    CommitData = ""
                }).Entity;
                Context.SaveChanges();
            }
            else
                commit = Context.Commits.FirstOrDefault(x=>x.CommitKey == commitKey);

            var branchCommitAssociation = default(AssociatedBranchCommits);
            var commitAssociationExist =Context.AssociatedBranchCommits.Include(x=>x.Commit).FirstOrDefault(x=>x.Commit.CommitKey == commitKey);
            if(commitAssociationExist != null)
                branchCommitAssociation = commitAssociationExist;
            else
            {
                branchCommitAssociation = Context.AssociatedBranchCommits.Add(new AssociatedBranchCommits{
                    BranchId = id, 
                    CommitId = commit.Id,
                }).Entity;
                Context.SaveChanges();
            }
            commitFiles.ForEach(x=>
            {
                var file = x;
                if(!Context.AssociatedCommitFiles.Include(y=>y.File)
                                                .Include(y=>y.Commit)
                                                .Any(y=>y.Commit.CommitKey == commitKey && file.FilePath == y.File.FilePath))
                {
                    var fileExist = Context.Files.FirstOrDefault(y=>  y.DateOfFile == file.DateOfFile
                                                && y.CurrentName == file.CurrentName
                                                && y.FilePath == file.FilePath);
                    
                    var cFile = default(Files);
                    if(fileExist != null)
                        cFile = fileExist;
                    else
                        cFile = Context.Files.Add(new Files{
                            CurrentName = file.CurrentName,
                            FilePath = file.FilePath,
                            FileData = "",
                            DateOfFile = file.DateOfFile,

                        }).Entity;
                    Context.AssociatedCommitFiles.Add(new AssociatedCommitFiles{
                        CommitId = commit.Id,
                        FileId = cFile.Id,
                        DateOfCommit=  DateTime.Now
                    });
                    Context.SaveChanges();
                }
            });
        
        }
        internal void AssociatedCommitFilesWithExistingBranch (List<Files> commitFiles, int id, string commitKey,string commitedBy)
        {
            
            var newCommit = default(bool);
            var commit  =default(Commits);
            if(!Context.Commits.Any(x=>x.CommitKey == commitKey))
            {
                newCommit = true;
                commit =Context.Commits.Add(new Commits{
                    CommitKey = commitKey,
                    CommitedBy = commitedBy,
                    CommitData = "",
                    DateOfCommit = DateTime.Now        
                }).Entity;
                Context.SaveChanges();
            }
            commitFiles.ForEach(x=>{
                if(commit != null)
                {
                    var file = Context.Files.Add(x);
                    Context.SaveChanges();
                     
                    var associatedCommitFile = Context.AssociatedCommitFiles.Add(new AssociatedCommitFiles{
                        CommitId = commit.Id,
                        FileId = file.Entity.Id,
                        DateOfCommit = DateTime.Now
                    });
                    Context.SaveChanges();
                }
            });
            
            if(newCommit)
            {
                Context.AssociatedBranchCommits.Add(new AssociatedBranchCommits{
                    CommitId = commit.Id,
                    BranchId = id,
                });
                Context.SaveChanges();
            }
        }

        internal Branches GetBranch(string b, int id)
        {
            return Context.Branches.FirstOrDefault(x=>x.BranchName == b && x.ProjectId == id);
        }

        internal List<OutgoingAccountManagment> GetOutgoingManagmentAccounts()
        {
            return Context.UserAccounts.Include(x=>x.AssociatedProjectMembers)
                                        .ThenInclude(AssociatedProjectsMembers => AssociatedProjectsMembers.Project)
                                        .Select(x=> new OutgoingAccountManagment{
                                            AccountId = x.Id,
                                            Name = $"{x.FirstName} {x.LastName}",
                                            Type = x.ProjectRights == 1 ? "Regular" : "Administrator",
                                            Email = x.Email,
                                            CreationDate = x.CreationDate,
                                            Projects = GetJsonData(x.AssociatedProjectMembers.Select(y=>y.Project).ToList())
                                        }).ToList();
        }
 

        internal List<AssociatedBoardWorkItems> GetProjectWorkItems(int id)
        {
            var items = Context.AssociatedBoardWorkItems.Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.WorkItemType)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.State)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssignedAccountNavigation)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemDuplicatesWorkItem)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemPredecessorsWorkItem)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemRelatedWorkItem)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemSuccessorsWorkItem)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssociatedWorkItemTestsWorkItem) 
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssociatedWrorkItemChildrenWorkItem)
                                                   .Include(x=>x.WorkItem)
                                                   .ThenInclude(WorkItem => WorkItem.AssociatedWrorkItemParentsWorkItem)                                                 
                                                   .Where(x=>x.ProjectId == id)
                                                   .ToList();
            return  items;
        }

        internal object GetProjects()
        {
            return Context.Projects.Select(x=> new{
                Name = x.ProjectName,
                Id = x.Id
            }).ToList();
        }

        internal OutgoingProjectRules GetProjectRules(int id, int userId)
        {
            var projectDetails =  Context.AssociatedProjectMembers.FirstOrDefault(x=>x.ProjectId == id && x.UserAccountId == userId);
            return new OutgoingProjectRules{
                CanClone = projectDetails.CanClone == 1 ? true : false,
                CanView  =projectDetails.CanViewWork == 1 ? true : false,
                CanCommit =projectDetails.CanCommit == 1 ? true : false,
                CanCreateWork = projectDetails.CanCreateWork == 1 ? true : false,
                CanDeleteWork =projectDetails.CanDeleteWork == 1 ? true : false,
            };
        }

        internal List<CommitChartBindingData> GetCommitsChartForProject(int id)
        {
            return Context.AssociatedBranchCommits.Where(x=>x.Branch.ProjectId == id).Select(x=> new CommitChartBindingData{
                DateOfCommit = x.Commit.DateOfCommit.Value,
                DayCount = Context.Commits.Where(y=>y.DateOfCommit == x.Commit.DateOfCommit).Count(),
            }).ToList();
        }

        internal Projects GetProjectData(int id)
        {
 
            return Context.Projects.Include(x=>x.Branches)
                                   .ThenInclude(Brances => Brances.AssociatedBranchCommits)
                                   .ThenInclude(AssociatedBranchCommits => AssociatedBranchCommits.Commit)
                                   .FirstOrDefault(x=>x.Id == id);

        }

        internal void UpdateUserAccount(IncomingUserAccountUpdate userData)
        {
            var userAccount = Context.UserAccounts.FirstOrDefault(x=>x.Id == userData.Id);
            Context.Attach(userAccount);
            userAccount.Email = userData.Email;
            if(!string.IsNullOrEmpty(userData.Password))
            {
                userAccount.Password = userData.Password;
                Context.Entry(userAccount).Property("Password").IsModified = true;

            }
            userAccount.FirstName = userData.FirstName;
            userAccount.LastName = userData.LastName;
            Context.Entry(userAccount).Property("FirstName").IsModified = true;
            Context.Entry(userAccount).Property("LastName").IsModified = true;
            Context.Entry(userAccount).Property("Email").IsModified = true;
            Context.SaveChanges();      
        }

        internal void UpdateUserProjectRights(IncomignRuleUpdate projectRuleData)
        {
            var userAccount = Context.UserAccounts.FirstOrDefault(x=>x.Id == projectRuleData.UserId);
            Context.Attach(userAccount);
            userAccount.ProjectRights = projectRuleData.IncomingValue ? 1:0;
            Context.Entry(userAccount).Property("ProjectRights").IsModified = true;
            Context.SaveChanges();
        }

        internal void RemoveUserFromProject(IncomingRemoveUserFromProject userProject)
        {
            var userAccount = Context.AssociatedProjectMembers.FirstOrDefault(x=>x.ProjectId == userProject.ProjectId && x.UserAccountId == userProject.UserId);
            Context.Attach(userAccount);
            Context.Remove(userAccount);
            Context.SaveChanges();
        }
        internal void UpdateProjectUserRule(IncomignRuleUpdate projectRuleData, string rule)
        {
            var getProject = Context.AssociatedProjectMembers
                                    .FirstOrDefault(x=>x.ProjectId == projectRuleData.ProjId
                                    && x.UserAccountId == projectRuleData.UserId);
            Context.Attach(getProject);
            switch(rule)
            {  
                case "CommitRule":

                        getProject.CanCommit = projectRuleData.IncomingValue ? 1 :0;
                        Context.Entry(getProject).Property("CanCommit").IsModified = true;
                    break;
                case "CloneRule":
                    getProject.CanClone = projectRuleData.IncomingValue ? 1 :0;
                    Context.Entry(getProject).Property("CanClone").IsModified = true;


                    break;
                case "ViewWorkRule":
                    getProject.CanViewWork = projectRuleData.IncomingValue ? 1 :0;
                    Context.Entry(getProject).Property("CanViewWork").IsModified = true;


                    break;
                case "CreateWorkRule":
                    getProject.CanCreateWork = projectRuleData.IncomingValue ? 1 :0;
                    Context.Entry(getProject).Property("CanCreateWork").IsModified = true;

                    break;
                case "DeleteWorkRule":
                    getProject.CanDeleteWork = projectRuleData.IncomingValue ? 1 :0;
                    Context.Entry(getProject).Property("CanDeleteWork").IsModified = true;

                    break;
            }
            Context.SaveChanges();

        }

        internal int AddUserAccount(IncomingNewUserAccount user)
        {
            var account = Context.UserAccounts.Add(new UserAccounts{
                    Email = user.Email,
                    Password = user.Password,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    ProjectRights = user.ProjectRights? 1 :0,
                    CreationDate = DateTime.Now
            });
            Context.SaveChanges();
            return account.Entity.Id;
        }

        internal OutgoingAccountManagment GetSpecificUserEdit(int id)
        {
            return Context.UserAccounts.Include(x=>x.AssociatedProjectMembers)
                                        .ThenInclude(AssociatedProjectsMembers => AssociatedProjectsMembers.Project)
                                        .Select(x=> new OutgoingAccountManagment{
                                            AccountId = x.Id,
                                            Name = $"{x.FirstName} {x.LastName}",
                                            Type = x.ProjectRights == 1 ? "Regular" : "Administrator",
                                            Email = x.Email,
                                            FirstName = x.FirstName,
                                            LastName = x.LastName,
                                            CreationDate = x.CreationDate,
                                            ProjectRights = x.ProjectRights == 1 ? true: false,
                                            Projects = $@"{GetJsonData(x.AssociatedProjectMembers.Select(y=>y.Project).ToList())}"
                                        }).FirstOrDefault(x=>x.AccountId == id);       
        }

        internal void AddProjectToUser(IncomingProjectUser incomingRequest)
        {
            var project = Context.Projects.FirstOrDefault(x=>x.Id == incomingRequest.Id);
            Context.AssociatedProjectMembers.Add(new  AssociatedProjectMembers{
                ProjectId = incomingRequest.Id,
                UserAccountId = incomingRequest.UserId,
                RepositoryId = project.RepositoryId
            });
            Context.SaveChanges();
        }

        private string GetJsonData(Object currnet)
        {
            return JsonConvert.SerializeObject(currnet);
        }

        internal List<BindingUserAccount> GetProjectMembers(int projectId)
        {
            return Context.UserAccounts
            .Include(x=> x.AssociatedProjectMembers)
            .Where(x=>x.AssociatedProjectMembers.Any(y=>y.ProjectId == projectId)).Select(x=> new BindingUserAccount{
                Email = x.Email,
                AliasName = $"{x.FirstName.FirstOrDefault()}.{x.LastName.FirstOrDefault()}",
                Id = x.Id
            })
            .ToList();
        }

        internal List<Risks> GetProjectRisks(int projectId)
        {
            return Context.Risks.ToList();
        }

        internal List<ValueAreas> GetProjectValueAreas(int projectId)
        {
            return Context.ValueAreas.ToList();
        }

        internal bool UpdateWorkItem(IncomingWorkItem currentItem) => WorkItemHadler.UpdateWorkItem(currentItem,Context);

        internal bool AddNewProject(IncomingProject currentProject, int v)
        {
            if(Context.UserAccounts.FirstOrDefault(x=>x.Id == v).ProjectRights == 1)
            {
                var userAccounts = Context.UserAccounts.Where(x=>currentProject.Users.Any(y=>y.AccountId == x.Id)).ToList();
                var repoStatus = RepositoryManager.AddNewProject($"/home/GitRepositories/",currentProject.ProjectName, userAccounts);
                if(repoStatus)
                {
                     var repository = Context.Repository.Add(new Repository{
                        FolderPath = $"/home/GitRepositories/{currentProject.ProjectName}"
                    });
                    Context.SaveChanges();
                    var boardBacklog = Context.Boards.Add(new Boards{
                        RepositoryId = repository.Entity.Id,
                        BoardType = 1,
                        BoardName = "Open"
                    });
                    var boardActive = Context.Boards.Add(new Boards{
                        RepositoryId = repository.Entity.Id,
                        BoardType = 2,
                        BoardName = "InProgress"
                    });
                    var boardTesting = Context.Boards.Add(new Boards{
                        RepositoryId = repository.Entity.Id,
                        BoardType = 3,
                        BoardName = "Testing"
                    });
                    var boardDone = Context.Boards.Add(new Boards{
                        RepositoryId = repository.Entity.Id,
                        BoardType = 4,
                        BoardName = "Done"
                    });

                   
                    Context.SaveChanges();
                    var project = Context.Projects.Add(new Projects{
                        ProjectDescription = currentProject.ProjectDescription,
                        ProjectName = currentProject.ProjectName,
                        ProjectTitle = "",
                        RepositoryId = repository.Entity.Id,
                        BoardId = boardBacklog.Entity.Id
                    });
                    Context.SaveChanges();
                    Context.AssociatedProjectBoards.Add(new AssociatedProjectBoards{
                        ProjectId = project.Entity.Id,
                        BoardId = boardBacklog.Entity.Id,
                        Position = 1
                    });
                    Context.AssociatedProjectBoards.Add(new AssociatedProjectBoards{
                        ProjectId = project.Entity.Id,
                        BoardId = boardActive.Entity.Id,
                        Position = 2
                    });
                    Context.AssociatedProjectBoards.Add(new AssociatedProjectBoards{
                        ProjectId = project.Entity.Id,
                        BoardId = boardTesting.Entity.Id,
                        Position = 3
                    });
                    Context.AssociatedProjectBoards.Add(new AssociatedProjectBoards{
                        ProjectId = project.Entity.Id,
                        BoardId = boardDone.Entity.Id,
                        Position = 4
                    });
                    Context.SaveChanges();
                    currentProject.Users.ForEach(x=>{
                        Context.AssociatedProjectMembers.Add(new AssociatedProjectMembers{
                            ProjectId = project.Entity.Id,
                            RepositoryId = repository.Entity.Id,
                            UserAccountId = x.AccountId,
                            CanClone = 1,
                            CanCommit = 1,
                            CanCreateWork = 1,
                            CanDeleteWork = 1,
                            CanViewWork = 1
                        });
                        Context.SaveChanges();
                    });
                   

                }
               
            }
           return true;
        }
        internal bool AddNewWorkItem (IncomingWorkItem currentItem)
        {
            return WorkItemHadler.AddNewWorkItem(currentItem,Context);
        
        }
        internal List<WorkItemRelations> GetProjectRelationships()
        {
            return Context.WorkItemRelations.ToList();
        }

        internal List<WorkItemActivity> GetProjectActivities(int projectId)
        {
            return Context.WorkItemActivity
            .ToList();     
        }

        internal List<WorkItemSeverities> GetProjectSeverities(int projectId)
        {
            return Context.WorkItemSeverities
            .ToList();     
        }

        internal List<WorkItemIterations> GetProjectIterations(int projectId)
        {
            return Context.WorkItemIterations
            .ToList();       
        }

        internal WorkItem ValidateWorkItemConnection(IncomignWorkItemRelation incomingRequest)
        {
            return Context.WorkItem.FirstOrDefault(x=> x.PriorityId == incomingRequest.ProjectId && x.Id == incomingRequest.WorkItemId);
        }

        internal List<WorkItemAreas> GetProjectAreas(int projectId)
        {
            return Context.WorkItemAreas
            .ToList();        
        }
        internal List<WorkItemReasons> GetProjectReasons(int projectId)
        {
            return Context.WorkItemReasons
            .ToList();        
        }

        internal List<WorkItemPriorities> GetProjectPriorities(int projectId)
        {
            return Context.WorkItemPriorities
            .ToList();
        }

        internal List<Builds> GetProjectBuilds(int projectId)
        {
            return Context.AssociatedProjectBuilds
            .Where(x=>x.ProjectId == projectId)
            .Select(x=> x.Build)
            .ToList();
        }

        internal string GetUsername(int currentId)
        {
            var account = Context.UserAccounts.FirstOrDefault(x=> x.Id == currentId);
            return $"{account.FirstName} {account.LastName}";
        }

        internal List<Projects> GetUserProjects(int id)
        {
            return Context.Projects.Include(x=>x.AssociatedProjectMembers)
                                    .ThenInclude(AssociatedProjectMembers => AssociatedProjectMembers.UserAccount)
                                    .Where(x=> x.AssociatedProjectMembers.Any(y=> y.UserAccountId == id)).ToList();
        }
        public List<string> Execute(string shPath,string repoPath)
        {
            System.Console.WriteLine(shPath);
            System.Console.WriteLine(repoPath);
             var current = OS.GetCurrent();
            System.Console.WriteLine(current);
            if(current == "gnu")
            { 
                try{
                    var cmdResult = RepositoryManager.ExecuteCmd("/bin/bash", $"{shPath} {repoPath}");
                    var data =cmdResult.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    System.Console.WriteLine(cmdResult);  
                    return data;
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine(ex);
                  //  return false;
                }
            }
            return null;
        }
        public string ExecuteSingle(string shPath,string repoPath)
        {
            System.Console.WriteLine(shPath);
            System.Console.WriteLine(repoPath);
             var current = OS.GetCurrent();
            System.Console.WriteLine(current);
            if(current == "gnu")
            { 
                try{
                    return RepositoryManager.ExecuteCmd("/bin/bash", $"{shPath} {repoPath}");
                }
                catch(Exception ex)
                {
                    System.Console.WriteLine(ex);
                  //  return false;
                }
            }
            return string.Empty;
        }

        internal Repository GetRepositoryByName(string rName)
        {
            var projectExist = Context.Projects.Include(x=>x.Repository).FirstOrDefault(x=>x.ProjectName == rName);
            if(projectExist !=null)
                return projectExist.Repository;
            
            return null;
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }


        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DatabaseController()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion

    }
}