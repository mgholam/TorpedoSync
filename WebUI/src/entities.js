Share = function () {
    this.Name = "";
    this.Path = "";
    this.ReadOnlyToken = $.uuid();
    this.ReadWriteToken = $.uuid();
};


Connection = function () {
    this.Name = ""; // share name
    this.Path = ""; // share path on disk
    this.MachineName = ""; // machine name to connect to 
    this.Token = "";
    this.isConfirmed = false; // share confirmed for sync
    this.ReadOnly = true;
    this.isPaused = false;
    this.isClient = true;
};

ConnectionInfo = function () {
    this.TotalFileCount = 0;
    this.FilesInQue = 0;
    this.FailedFiles = [];
    this.LastFileNameDownloaded = "";
    this.QueDataSize = 0;
    this.Mbs = 0;
    this.EstimatedTimeSecs = 0;
    this.Que = [];
    this.Failed = [];
};