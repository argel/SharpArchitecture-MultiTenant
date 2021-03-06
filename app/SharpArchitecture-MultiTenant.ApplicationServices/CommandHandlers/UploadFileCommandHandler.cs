﻿using System;
using SharpArch.Domain.Commands;
using SharpArch.Domain.PersistenceSupport;
using SharpArchitecture.MultiTenant.ApplicationServices.Commands;
using SharpArchitecture.MultiTenant.Core;

namespace SharpArchitecture.MultiTenant.ApplicationServices.CommandHandlers
{
  public class UploadFileCommandHandler : ICommandHandler<UploadFileCommand>
  {
    private readonly IFileStore _fileStore;
    private readonly IRepository<Upload> _uploadRepository;

    public UploadFileCommandHandler(IFileStore fileStore, IRepository<Upload> uploadRepository)
    {
      _fileStore = fileStore;
      _uploadRepository = uploadRepository;
    }

    public ICommandResult Handle(UploadFileCommand command)
    {
      try {
        var upload = new Upload(command.FileData.FileName, command.GroupId, command.Username);
        upload.UploadedPath = _fileStore.SaveUploadedFile(upload.UploadedFilename, command.FileData);
        _uploadRepository.SaveOrUpdate(upload);

        return new UploadFileCommandResult(true);
      }
      catch (Exception ex) {
        return new UploadFileCommandResult(false) { Message = "A problem was encountered uploading the file: " + ex.Message };
      }
    }
  }
}