CREATE TABLE T_ACCOUNT (
	[AccountId] [int] IDENTITY(1,1) NOT NULL,
	[RingCentralId] [varchar](100) NOT NULL DEFAULT (''),
	[RingCentralOwnerId] [varchar](100) NOT NULL DEFAULT (''),
	[NameOnRingCentralAccount] [varchar](300) NOT NULL DEFAULT (''),
	[DisplayName] [varchar](200) NOT NULL DEFAULT (''),
	[RingCentralUsername] [varchar](1000) NOT NULL DEFAULT (''),
	[RingCentralExtension] [varchar](100) NOT NULL DEFAULT (''),
	[RingCentralTokenId] [int] NOT NULL DEFAULT ((0)),
	[LastLogin] [datetime] NULL,
	[StripeCustomerId] [varchar](100) NOT NULL DEFAULT '',
	[StripeSubscriptionId] [varchar](100) NOT NULL DEFAULT '',
	[PlanId] [varchar](100) NOT NULL DEFAULT '',
	[DeletedInd] [bit] NOT NULL DEFAULT ((0)),
	[ActiveInd] [bit] NOT NULL DEFAULT ((1)),
	[RegisteredInd] [bit] NOT NULL DEFAULT ((1)),
	[CancelledInd] [bit] NOT NULL DEFAULT ((1)),
	[PaymentIsCurrentInd] [bit] NOT NULL DEFAULT ((1))
)
ALTER TABLE T_ACCOUNT ADD [RingCentralId] [varchar](100) NOT NULL DEFAULT ('')
ALTER TABLE T_ACCOUNT ADD [RingCentralOwnerId] [varchar](100) NOT NULL DEFAULT ('')
ALTER TABLE T_ACCOUNT ADD [NameOnRingCentralAccount] [varchar](300) NOT NULL DEFAULT ('')
ALTER TABLE T_ACCOUNT ADD [DisplayName] [varchar](200) NOT NULL DEFAULT ('')

CREATE TABLE T_RINGCENTRALTOKEN (
	[RingCentralTokenId] [int] IDENTITY(1,1) NOT NULL,
	[AccessToken] [varchar](1000) NOT NULL DEFAULT (''),
	[IdToken] [varchar](1000) NOT NULL DEFAULT (''),
	[ExpiresIn] [varchar](1000) NOT NULL DEFAULT (''),
	[TokenType] [varchar](1000) NOT NULL DEFAULT (''),
	[RefreshToken] [varchar](1000) NOT NULL DEFAULT (''),
	[RefreshTokenExpiresIn] [varchar](1000) NOT NULL DEFAULT (''),
	[EndpointId] [varchar](1000) NOT NULL DEFAULT (''),
	[OwnerId] [varchar](1000) NOT NULL DEFAULT (''),
	[Scope] [varchar](1000) NOT NULL DEFAULT (''),
	[LastRefreshedOn] [DateTime] NOT NULL,
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)
CREATE TABLE T_RINGCENTRALTOKENHISTORY (
	[RingCentralTokenHistoryId] [int] IDENTITY(1,1) NOT NULL,
	[AccessToken] [varchar](1000) NOT NULL DEFAULT (''),
	[ExpiresIn] [varchar](50) NOT NULL DEFAULT (''),
	[TokenType] [varchar](100) NOT NULL DEFAULT (''),
	[RefreshToken] [varchar](1000) NOT NULL DEFAULT (''),
	[RefreshTokenExpiresIn] [varchar](50) NOT NULL DEFAULT (''),
	[OwnerId] [varchar](20) NOT NULL DEFAULT (''),
	[Scope] [varchar](1000) NOT NULL DEFAULT (''),
	[LastRefreshedOn] [DateTime] NOT NULL
)


CREATE TABLE T_RINGCENTRALCONTACT (
	[RingCentralContactId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[RingCentralId] [varchar](100) NOT NULL DEFAULT (''),

	[Firstname] [varchar](200) NOT NULL DEFAULT (''),
	[Lastname] [varchar](200) NOT NULL DEFAULT (''),
	[Company] [varchar](400) NOT NULL DEFAULT (''),
	[Email] [varchar](500) NOT NULL DEFAULT (''),
	[BusinessPhone] [varchar](20) NOT NULL DEFAULT (''),

	[Street] [varchar](200) NOT NULL DEFAULT (''),
	[City] [varchar](200) NOT NULL DEFAULT (''),
	[State] [varchar](20) NOT NULL DEFAULT (''),
	[Zip] [varchar](20) NOT NULL DEFAULT (''),
	[Country] [varchar](200) NOT NULL DEFAULT (''),

	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

CREATE TABLE T_FTPACCOUNT (
	[FtpAccountId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[FtpAccountName] [varchar](1000) NOT NULL DEFAULT (''),
	[Uri] [varchar](2000) NOT NULL DEFAULT (''),
	[Username] [varchar](1000) NOT NULL DEFAULT (''),
	[Password] [varchar](1000) NOT NULL DEFAULT ('')
)

CREATE TABLE T_GOOGLEACCOUNT (
	[GoogleAccountId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[GoogleTokenId] [int] NOT NULL DEFAULT ((0)),
	[GoogleAccountName] [varchar](1000) NOT NULL DEFAULT (''),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0)),
	[ActiveInd] [bit] NOT NULL DEFAULT ((1))
)

CREATE TABLE T_GOOGLETOKEN (
	[GoogleTokenId] [int] IDENTITY(1,1) NOT NULL,
	[Email] [varchar](1000) NOT NULL DEFAULT (''),
	[AccessToken] [varchar](1000) NOT NULL DEFAULT (''),
	[IdToken] [varchar](1000) NOT NULL DEFAULT (''),
	[ExpiresIn] [varchar](1000) NOT NULL DEFAULT (''),
	[TokenType] [varchar](1000) NOT NULL DEFAULT (''),
	[RefreshToken] [varchar](1000) NOT NULL DEFAULT (''),
	[LastRefreshedOn] [DateTime] NOT NULL,
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

CREATE TABLE T_BOXACCOUNT (
	[BoxAccountId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[BoxTokenId] [int] NOT NULL DEFAULT ((0)),
	[BoxAccountName] [varchar](1000) NOT NULL DEFAULT (''),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0)),
	[ActiveInd] [bit] NOT NULL DEFAULT ((1))
)

CREATE TABLE T_BOXTOKEN (
	[BoxTokenId] [int] IDENTITY(1,1) NOT NULL,
	[Email] [varchar](1000) NOT NULL DEFAULT (''),
	[AccessToken] [varchar](1000) NOT NULL DEFAULT (''),
	[IdToken] [varchar](1000) NOT NULL DEFAULT (''),
	[ExpiresIn] [varchar](1000) NOT NULL DEFAULT (''),
	[TokenType] [varchar](1000) NOT NULL DEFAULT (''),
	[RefreshToken] [varchar](1000) NOT NULL DEFAULT (''),
	[LastRefreshedOn] [DateTime] NOT NULL,
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

CREATE TABLE T_AMAZONACCOUNT (
	[AmazonAccountId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[AmazonUserId] [int] NOT NULL DEFAULT ((0)),
	[AmazonAccountName] [varchar](1000) NOT NULL DEFAULT (''),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0)),
	[ActiveInd] [bit] NOT NULL DEFAULT ((1))
)

CREATE TABLE T_AMAZONUSER (
	[AmazonUserId] [int] IDENTITY(1,1) NOT NULL,
	[Region] [varchar](500) NOT NULL DEFAULT (''),
	[AccessKeyId] [varchar](1000) NOT NULL DEFAULT (''),
	[SecretAccessKey] [varchar](1000) NOT NULL DEFAULT (''),
	[DisplayName] [varchar](500) NOT NULL DEFAULT (''),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

CREATE TABLE T_TRANSFERRULE (
	[TransferRuleId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[Source] [varchar](30) NOT NULL DEFAULT (''),
	[Destination] [varchar](30) NOT NULL DEFAULT (''),
	[DestinationBoxAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationGoogleAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationAmazonAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationFtpAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationFolderId] [varchar](500) NULL,
	[DestinationFolderPath] [varchar](5000) NULL,
	[DestinationFolderName] [varchar](1000) NULL,
	[DestinationFolderLabel] [varchar](5000) NULL,
	[DestinationBucketName] [varchar](500) NULL,
	[DestinationPrefix] [varchar](5000) NULL,
	[PutInDatedSubFolder] [bit] NOT NULL DEFAULT ((0)),

	[VoiceLogInd] [bit] NOT NULL DEFAULT ((0)),
	[VoiceContentInd] [bit] NOT NULL DEFAULT ((0)),
	[FaxLogInd] [bit] NOT NULL DEFAULT ((0)),
	[FaxContentInd] [bit] NOT NULL DEFAULT ((0)),
	[SmsLogInd] [bit] NOT NULL DEFAULT ((0)),
	[SmsContentInd] [bit] NOT NULL DEFAULT ((0)),

	[Frequency] [varchar](50) NOT NULL DEFAULT (''),
	[DayOf] [varchar](50) NOT NULL DEFAULT (''),
	[TimeOfDay] [varchar](20) NOT NULL DEFAULT (''),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0)),
	[ActiveInd] [bit] NOT NULL DEFAULT ((1))
)
ALTER TABLE T_TRANSFERRULE ADD [DestinationAmazonAccountId] [int] NOT NULL DEFAULT ((0))
ALTER TABLE T_TRANSFERRULE ADD [DestinationBucketName] [varchar](500) NULL
ALTER TABLE T_TRANSFERRULE ADD [DestinationPrefix] [varchar](5000) NULL
ALTER TABLE T_TRANSFERRULE ADD [VoiceLogInd] [bit] NOT NULL DEFAULT (0)
ALTER TABLE T_TRANSFERRULE ADD [VoiceContentInd] [bit] NOT NULL DEFAULT (0)
ALTER TABLE T_TRANSFERRULE ADD [FaxLogInd] [bit] NOT NULL DEFAULT (0)
ALTER TABLE T_TRANSFERRULE ADD [FaxContentInd] [bit] NOT NULL DEFAULT (0)
ALTER TABLE T_TRANSFERRULE ADD [SmsLogInd] [bit] NOT NULL DEFAULT (0)
ALTER TABLE T_TRANSFERRULE ADD [SmsContentInd] [bit] NOT NULL DEFAULT (0)

CREATE TABLE T_TRANSFERBATCH (
	[TransferBatchId] [int] IDENTITY(1,1) NOT NULL,
	[TransferRuleId] [int] NOT NULL DEFAULT ((0)),
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[CreateDate] [datetime] NOT NULL,
	[QueuedInd] [bit] NOT NULL DEFAULT ((0)),
	[RedoInd] [bit] NOT NULL DEFAULT ((0)),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0)),
	[ProcessingInd] [bit] NOT NULL DEFAULT ((0)),
	[LogNumber] [int] NOT NULL DEFAULT ((0))
)
ALTER TABLE T_TRANSFERBATCH ADD [RedoInd] [bit] NOT NULL DEFAULT ((0))
ALTER TABLE T_TRANSFERBATCH ADD [ErrorInd] [bit] NOT NULL DEFAULT ((0))
ALTER TABLE T_TRANSFERBATCH ADD [CompleteDate] [datetime] NULL

CREATE TABLE T_TICKET (
	[TicketId] [int] IDENTITY(1,1) NOT NULL,
	[TransferBatchId] [int] NOT NULL DEFAULT ((0)),
	[CreateDate] [datetime] NOT NULL,
	[InitiatedBy] [varchar](30) NOT NULL DEFAULT (''),
	[Destination] [varchar](30) NOT NULL DEFAULT (''),
	[DestinationBoxAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationGoogleAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationAmazonAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationFtpAccountId] [int] NOT NULL DEFAULT ((0)),
	[DestinationFolderId] [varchar](500) NULL,
	[DestinationFolderLabel] [varchar](5000) NULL,
	[DestinationBucketName] [varchar](500) NULL,
	[DestinationPrefix] [varchar](5000) NULL,
	[PutInDatedSubFolder] [bit] NOT NULL DEFAULT ((0)),
	[SaveAsFileName] [varchar](500) NOT NULL DEFAULT (''),
	[MessageId] [varchar](50) NULL,
	[CallId] [varchar](50) NULL,
	[CallTime] [datetime] NULL,
	[LogInd] [bit] NOT NULL DEFAULT (0),
	[ContentInd] [bit] NOT NULL DEFAULT (0),
	[ProcessingInd] [bit] NOT NULL DEFAULT ((0)),
	[RedoInd] [bit] NOT NULL DEFAULT ((0)),
	[Type] [varchar](20) NOT NULL DEFAULT (''),
	[NameIteration] [int] NOT NULL DEFAULT ((1)),
	[CompleteInd] [bit] NOT NULL DEFAULT ((0)),
	[CompleteDate] [datetime] NULL,
	[ErrorInd] [bit] NOT NULL DEFAULT ((0)),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

ALTER TABLE T_TICKET ADD [DestinationAmazonAccountId] [int] NOT NULL DEFAULT ((0))
ALTER TABLE T_TICKET ADD [DestinationBucketName] [varchar](500) NOT NULL DEFAULT ('')
ALTER TABLE T_TICKET ADD [DestinationPrefix] [varchar](5000) NOT NULL DEFAULT ('')
ALTER TABLE T_TICKET ADD [RedoInd] [bit] NOT NULL DEFAULT ((0))
ALTER TABLE T_TICKET ADD [MessageId] [varchar](50) NOT NULL DEFAULT ('')
ALTER TABLE T_TICKET ADD [Type] [varchar](20) NOT NULL DEFAULT ('')
ALTER TABLE T_TICKET ADD [LogInd] [bit] NOT NULL DEFAULT ((0))
ALTER TABLE T_TICKET ADD [ContentInd] [bit] NOT NULL DEFAULT ((0))
ALTER TABLE T_TICKET ADD [NameIteration] [int] NOT NULL DEFAULT ((1))
ALTER TABLE T_TICKET ADD [CallTime] [datetime] NULL
ALTER TABLE T_TICKET ALTER COLUMN CallId [varchar](50) NULL
ALTER TABLE T_TICKET ADD [CompleteInd] [bit] NOT NULL DEFAULT ((0))
ALTER TABLE T_TICKET ADD [ErrorInd] [bit] NOT NULL DEFAULT ((0))
ALTER TABLE T_TICKET ADD [CompleteDate] [datetime] NULL

CREATE TABLE T_TICKETRAWDATA (
	[TicketRawDataId] [int] IDENTITY(1,1) NOT NULL,
	[TicketId] [int] NOT NULL DEFAULT ((0)),
	[TransferBatchId] [int] NOT NULL DEFAULT ((0)),
	[RawData] [text] NOT NULL DEFAULT(''),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

CREATE TABLE T_TRANSFERBATCHLOG (
	[TransferBatchLogId] [int] IDENTITY(1,1) NOT NULL,
	[TransferBatchId] [int] NOT NULL DEFAULT ((0)),
	[TransferBatchLogStartDate] [datetime] NULL,
	[TransferBatchLogStopDate] [datetime] NULL,
	[ErrorInd] [bit] NOT NULL DEFAULT ((0)),
	[Message] [varchar](500) NULL
)

CREATE TABLE T_TICKETLOG (
	[TicketLogId] [int] IDENTITY(1,1) NOT NULL,
	[TicketId] [int] NOT NULL DEFAULT ((0)),
	[TransferBatchId] [int] NOT NULL DEFAULT ((0)),
	[TicketLogStartDate] [datetime] NULL,
	[TicketLogStopDate] [datetime] NULL,
	[ErrorInd] [bit] NOT NULL DEFAULT ((0)),
	[Message] [varchar](500) NULL,
	[LogText] [varchar](5000) NULL
)

CREATE TABLE T_TICKETLOGTRACE (
	[TicketLogTrackingId] [int] IDENTITY(1,1) NOT NULL,
	[TicketLogId] [int] NOT NULL DEFAULT ((0)),
	[TicketId] [int] NOT NULL DEFAULT ((0)),
	[TransferBatchId] [int] NOT NULL DEFAULT ((0)),
	[TraceText] [text] NOT NULL DEFAULT (''),
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

CREATE TABLE T_ERROR (
    [ErrorId] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[Exception1] [varchar](5000) NOT NULL DEFAULT '',
	[Exception2] [varchar](5000) NOT NULL DEFAULT '',
	[Exception3] [varchar](5000) NOT NULL DEFAULT '',
	[Exception4] [varchar](5000) NOT NULL DEFAULT '',
	[Controller] [varchar](200) NOT NULL DEFAULT '',
	[Action] [varchar](200) NOT NULL DEFAULT ''
)
ALTER TABLE T_ERROR ADD [Controller] [varchar](200) NOT NULL DEFAULT ('')
ALTER TABLE T_ERROR ADD [Action] [varchar](200) NOT NULL DEFAULT ('')

CREATE TABLE T_OAUTHLOG (
    [OauthLogId] [int] IDENTITY(1,1) NOT NULL,
	[CreateDate] [datetime] NOT NULL,
	[LogText] Text NOT NULL DEFAULT '',
)

--RAWDATA
CREATE TABLE T_RINGCENTRALMESSAGERAWDATA (
	[RingCentralMessageRawDataId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[MessageId] [varchar](200) NOT NULL DEFAULT (''),
	[RawData] [text] NOT NULL DEFAULT '',
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)
CREATE TABLE T_RINGCENTRALCALLRAWDATA (
	[RingCentralCallRawDataId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[CallId] [varchar](200) NOT NULL DEFAULT (''),
	[RawData] [text] NOT NULL DEFAULT '',
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)


CREATE TABLE T_ANALYTICS (
	[AnalyticsId] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NOT NULL DEFAULT ((0)),
	[RefId] [varchar](100) NOT NULL DEFAULT (''),
	[RefPlan] [varchar](100) NOT NULL DEFAULT (''),
	[LoggedIn] [datetime] NULL,
	[ChoseFreePlanOnBillingPage] [datetime] NULL,
	[CompletedBilling] [datetime] NULL,
	[PresentedWithUpgradeViaAutomationUrl] [datetime] NULL,
	[PresentedWithUpgradeViaLetRingClone] [datetime] NULL,
	[SelectedUpgradePlan] [datetime] NULL,
	[CompletedUpgradeBilling] [datetime] NULL,
	[DeletedInd] [bit] NOT NULL DEFAULT ((0))
)

CREATE TABLE T_INDEX (
	IndexId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[OwnerId] [varchar](20) NOT NULL DEFAULT (0),
	[TicketId] [int] NOT NULL DEFAULT (0),
	[TicketLogId] [int] NOT NULL DEFAULT (0),
	[Type] [varchar](20) NOT NULL DEFAULT (''),
	[CallId] [varchar](30) NULL,
	[MessageId] [varchar](30) NULL,
	[IndexRawDataId] [int] NOT NULL DEFAULT (0),
	--INDEX/SEARCH
	[CallTime] [datetime] NOT NULL,
	[IndexMessageId] [int] NOT NULL DEFAULT (0),
	[Direction] [varchar](20) NULL,
	[Action] [varchar](20) NULL,
	[Result] [varchar](20) NULL,
	--GENERAL
	[DefaultFileName] [varchar](100) NOT NULL DEFAULT '',
	[TimeElapsedForTransfer] [int] NOT NULL DEFAULT (0),
	[TimeElapsedForIndex] [int] NOT NULL DEFAULT (0),
	[Duration] [int] NULL,
	[DeletedInd] [bit] NOT NULL DEFAULT (0),
	[CompleteInd] [bit] NOT NULL DEFAULT (0)
)
CREATE TABLE T_INDEXRAWDATA (
	[IndexRawDataId] [int] IDENTITY(1,1) NOT NULL,
	[RawData] [varchar](max) NOT NULL DEFAULT('')
)
CREATE TABLE T_INDEXCALLER (
	IndexCallerId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[IndexId] [int] NOT NULL DEFAULT (0),
	[PhoneNumber] [varchar](20) NULL,
	[ExtensionNumber] [varchar](10) NULL,
	[Name] [varchar](300) NULL,
	[Location] [varchar](300) NULL,
	[ToInd] [bit] NOT NULL DEFAULT (0),--DEFAULT IS "FROM"
	[DeletedInd] [bit] NOT NULL DEFAULT (0)
)
CREATE TABLE T_INDEXMESSAGE (
	IndexMessageId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Subject] [varchar](5000) NULL,
	[CoverPageText] [varchar](1024) NULL,
	[MessageStatus] [varchar](20) NULL,
	[FaxPageCount] [int] NULL
)
CREATE TABLE T_INDEXFILE (
	--GENERAL
	IndexFileId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[IndexId] [int] NOT NULL DEFAULT (0),
	--FILE
	[LogInd] [bit] NOT NULL DEFAULT (0),
	[ContentInd] [bit] NOT NULL DEFAULT (0),
	[IndexFileInfoId] [int] NOT NULL DEFAULT (0),
	--RETRIEVAL
	[Destination] [varchar](20) NOT NULL DEFAULT (''),
	[DestinationAccountId] [int] NOT NULL DEFAULT (0),
	[ContentUri] [varchar](700) NOT NULL DEFAULT (''),
	--STATS
	[NumberOfBytes] [int] NOT NULL DEFAULT (0),
	--GENERAL
	[DeletedInd] [bit] NOT NULL DEFAULT (0)
)
CREATE TABLE T_INDEXFILEINFO (
	IndexFileInfoId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Filename] [varchar](200) NULL,
	[FileId] [varchar](50) NULL ,
	[Folder] [varchar](1500) NULL,
	[BucketName] [varchar](200) NULL,
	[Prefix] [varchar](1500) NULL
)


--ODWNLOAD STUFF

CREATE TABLE T_DOWNLOAD (
	[DownloadId] varchar(40) NOT NULL PRIMARY KEY,
	[AccountId] [INT] NOT NULL DEFAULT 0,
	[DownloadDataId] [INT] NOT NULL DEFAULT 0,
	[DownloadModelId] [INT] NOT NULL DEFAULT 0,
	[CreateDate] [datetime] NOT NULL,
	[Filename] [varchar](200) NULL,
	[Tooltip] [varchar](800) NULL,
	[Percent] [int] NOT NULL DEFAULT 0,
	[ErrorInd] [bit] NOT NULL DEFAULT 0,
	[CompleteInd] [bit] NOT NULL DEFAULT 0,
	[SeenInd] [bit] NOT NULL DEFAULT 0,
	[DownloadedInd] [bit] NOT NULL DEFAULT 0,
	[DeletedInd] [bit] NOT NULL DEFAULT 0
)
CREATE TABLE T_DOWNLOADMODEL (
	[DownloadModelId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Model] [varchar](max) NULL
)
CREATE TABLE T_DOWNLOADDATA (
	[DownloadDataId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[Data] [varbinary](max) NULL
)
CREATE TABLE T_DOWNLOADERROR (
	[DownloadErrorId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[DownloadId] varchar(40) NOT NULL,
	[ErrorDate] [datetime] NOT NULL,
	[Error] [varchar](max) NULL
)


--WEB HOOK STUFF
CREATE TABLE T_WEBHOOKRAWDATA (
	[WebHookRawDataId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[RawData] [varchar](max) NOT NULL DEFAULT('')
)


--SEARCH
DECLARE @name varchar(100);
DECLARE @extension varchar(50);
DECLARE @ownerId varchar(50);
DECLARE @type varchar(50);
DECLARE @offset int;
DECLARE @perPage int;
DECLARE @dateFrom datetime;
DECLARE @dateTo datetime;
SET @ownerId = '386509065';
SET @type = 'voice';
SET @name = 'fox';
SET @extension = '9039';
SET @offset = 2;
SET @perPage = 50;
SET @dateFrom = '1/1/1900';
SET @dateTo = '1/1/2020';

--EXEC IndexSearchWithNameAndExtension @ownerId,@dateFrom,@dateTo,@name,@extension,@offset,@perPage
--EXEC IndexSearchWithName @ownerId,@dateFrom,@dateTo,@name,@offset,@perPage
EXEC IndexSearchWithExtension @ownerId,@dateFrom,@dateTo,@extension,@offset,@perPage


ALTER PROC IndexSearch
 @ownerId varchar(50)
,@type varchar(50)
,@dateFrom datetime
,@dateTo datetime
,@offset int
,@perPage int

AS

SELECT *
FROM (
   SELECT *, COUNT(*) OVER () AS TotalRecs
   FROM (
      SELECT T_INDEX.*,T_INDEXMESSAGE.Subject,T_INDEXMESSAGE.CoverPageText,T_INDEXMESSAGE.MessageStatus,T_INDEXMESSAGE.FaxPageCount
         FROM T_INDEX
         LEFT OUTER JOIN T_INDEXMESSAGE ON T_INDEX.IndexMessageId = T_INDEXMESSAGE.IndexMessageId
         WHERE OwnerId=@ownerId
         AND [Type]=@type
         AND (@dateFrom IS NULL OR CallTime >= @dateFrom)
         AND (@dateTo IS NULL OR CallTime <= @dateTo)
   ) AS t1
) AS t2
ORDER BY CallTime DESC
OFFSET (@offset) ROWS FETCH NEXT @perPage ROWS ONLY


ALTER PROC IndexSearchWithNameAndExtension
 @ownerId varchar(50)
,@type varchar(50)
,@dateFrom datetime
,@dateTo datetime
,@name varchar(100)
,@extension varchar(50)
,@offset int
,@perPage int

AS

SELECT *
FROM (
   SELECT *, COUNT(*) OVER () AS TotalRecs
   FROM (
      SELECT i.*
      FROM T_INDEX i
      WHERE EXISTS (SELECT 1
              FROM T_INDEXCALLER ic
              WHERE (charindex(@name COLLATE Latin1_General_CI_AI, Name COLLATE Latin1_General_CI_AI)>0 OR charindex(@name COLLATE Latin1_General_CI_AI, PhoneNumber COLLATE Latin1_General_CI_AI)>0 OR charindex(@name COLLATE Latin1_General_CI_AI, ExtensionNumber COLLATE Latin1_General_CI_AI)>0) AND i.IndexId = ic.IndexId
             )
         AND EXISTS (SELECT 1
              FROM T_INDEXCALLER ic
              WHERE ic.ExtensionNumber = @extension AND i.IndexId = ic.IndexId
             )
         AND OwnerId=@ownerId
         AND [Type]=@type
         AND (@dateFrom IS NULL OR CallTime >= @dateFrom)
         AND (@dateTo IS NULL OR CallTime <= @dateTo)
   ) AS t1
) AS t2
ORDER BY CallTime DESC
OFFSET (@offset) ROWS FETCH NEXT @perPage ROWS ONLY


ALTER PROC IndexSearchWithName
 @ownerId varchar(50)
,@type varchar(50)
,@dateFrom datetime
,@dateTo datetime
,@name varchar(100)
,@offset int
,@perPage int

AS

SELECT *
FROM (
   SELECT *, COUNT(*) OVER () AS TotalRecs
   FROM (
      SELECT DISTINCT T_INDEX.*
         FROM T_INDEX
         INNER JOIN T_INDEXCALLER ON T_INDEX.IndexId = T_INDEXCALLER.IndexId
         WHERE OwnerId=@ownerId
         AND [Type]=@type
         AND (charindex(@name COLLATE Latin1_General_CI_AI, Name COLLATE Latin1_General_CI_AI)>0 OR charindex(@name COLLATE Latin1_General_CI_AI, PhoneNumber COLLATE Latin1_General_CI_AI)>0 OR charindex(@name COLLATE Latin1_General_CI_AI, ExtensionNumber COLLATE Latin1_General_CI_AI)>0)
         AND (@dateFrom IS NULL OR CallTime >= @dateFrom)
         AND (@dateTo IS NULL OR CallTime <= @dateTo)
   ) AS t1
) AS t2
ORDER BY CallTime DESC
OFFSET (@offset) ROWS FETCH NEXT @perPage ROWS ONLY


ALTER PROC IndexSearchWithExtension
 @ownerId varchar(50)
,@type varchar(50)
,@dateFrom datetime
,@dateTo datetime
,@extension varchar(50)
,@offset int
,@perPage int

AS

SELECT *
FROM (
   SELECT *, COUNT(*) OVER () AS TotalRecs
   FROM (
      SELECT DISTINCT T_INDEX.*,T_INDEXMESSAGE.Subject,T_INDEXMESSAGE.CoverPageText,T_INDEXMESSAGE.MessageStatus,T_INDEXMESSAGE.FaxPageCount
         FROM T_INDEX
         INNER JOIN T_INDEXCALLER ON T_INDEX.IndexId = T_INDEXCALLER.IndexId
         LEFT OUTER JOIN T_INDEXMESSAGE ON T_INDEX.IndexMessageId = T_INDEXMESSAGE.IndexMessageId
         WHERE OwnerId=@ownerId
         AND [Type]=@type
         AND ExtensionNumber = @extension
         AND (@dateFrom IS NULL OR CallTime >= @dateFrom)
         AND (@dateTo IS NULL OR CallTime <= @dateTo)
   ) AS t1
) AS t2
ORDER BY CallTime DESC
OFFSET (@offset) ROWS FETCH NEXT @perPage ROWS ONLY


ALTER PROC IndexSearchWithNameAndExtensionAndSubject
 @ownerId varchar(50)
,@type varchar(50)
,@dateFrom datetime
,@dateTo datetime
,@name varchar(100)
,@extension varchar(50)
,@offset int
,@perPage int

AS

SELECT *
FROM (
   SELECT *, COUNT(*) OVER () AS TotalRecs
   FROM (
      SELECT i.*,T_INDEXMESSAGE.Subject,T_INDEXMESSAGE.CoverPageText,T_INDEXMESSAGE.MessageStatus,T_INDEXMESSAGE.FaxPageCount
      FROM T_INDEX i
      LEFT OUTER JOIN T_INDEXMESSAGE ON i.IndexMessageId = T_INDEXMESSAGE.IndexMessageId
      WHERE 
         (EXISTS (SELECT 1
              FROM T_INDEXCALLER ic
              WHERE (charindex(@name COLLATE Latin1_General_CI_AI, Name COLLATE Latin1_General_CI_AI)>0 OR charindex(@name COLLATE Latin1_General_CI_AI, PhoneNumber COLLATE Latin1_General_CI_AI)>0 OR charindex(@name COLLATE Latin1_General_CI_AI, ExtensionNumber COLLATE Latin1_General_CI_AI)>0) AND i.IndexId = ic.IndexId
             )
          OR (charindex(@name COLLATE Latin1_General_CI_AI, Subject COLLATE Latin1_General_CI_AI)>0))
         AND EXISTS (SELECT 1
              FROM T_INDEXCALLER ic
              WHERE ic.ExtensionNumber = @extension AND i.IndexId = ic.IndexId
             )
         AND OwnerId=@ownerId
         AND [Type]=@type
         AND (@dateFrom IS NULL OR CallTime >= @dateFrom)
         AND (@dateTo IS NULL OR CallTime <= @dateTo)
   ) AS t1
) AS t2
ORDER BY CallTime DESC
OFFSET (@offset) ROWS FETCH NEXT @perPage ROWS ONLY



ALTER PROC IndexSearchWithNameAndSubject
 @ownerId varchar(50)
,@type varchar(50)
,@dateFrom datetime
,@dateTo datetime
,@name varchar(100)
,@offset int
,@perPage int

AS

SELECT *
FROM (
   SELECT *, COUNT(*) OVER () AS TotalRecs
   FROM (
      SELECT DISTINCT T_INDEX.*,T_INDEXMESSAGE.Subject,T_INDEXMESSAGE.CoverPageText,T_INDEXMESSAGE.MessageStatus,T_INDEXMESSAGE.FaxPageCount
         FROM T_INDEX
         LEFT OUTER JOIN T_INDEXCALLER ON T_INDEX.IndexId = T_INDEXCALLER.IndexId
         LEFT OUTER JOIN T_INDEXMESSAGE ON T_INDEX.IndexMessageId = T_INDEXMESSAGE.IndexMessageId
         WHERE OwnerId=@ownerId
         AND [Type]=@type
         AND (charindex(@name COLLATE Latin1_General_CI_AI, Name COLLATE Latin1_General_CI_AI)>0 
          OR charindex(@name COLLATE Latin1_General_CI_AI, PhoneNumber COLLATE Latin1_General_CI_AI)>0 
          OR charindex(@name COLLATE Latin1_General_CI_AI, ExtensionNumber COLLATE Latin1_General_CI_AI)>0
          OR charindex(@name COLLATE Latin1_General_CI_AI, Subject COLLATE Latin1_General_CI_AI)>0)
         AND (@dateFrom IS NULL OR CallTime >= @dateFrom)
         AND (@dateTo IS NULL OR CallTime <= @dateTo)
   ) AS t1
) AS t2
ORDER BY CallTime DESC
OFFSET (@offset) ROWS FETCH NEXT @perPage ROWS ONLY







--REDO SCRIPT
DECLARE @BATCH INT;
SET @BATCH=5605
UPDATE T_TICKET SET REDOIND=1 WHERE (SELECT COUNT(*) FROM T_TICKETLOG WHERE T_TICKET.TICKETID=T_TICKETLOG.TICKETID AND ERRORIND=0) = 0 AND TRANSFERBATCHID = @BATCH
UPDATE T_TRANSFERBATCH SET REDOIND=1,QUEUEDIND=1 WHERE TRANSFERBATCHID=@BATCH


--ALL TABLES BY ACCOUNT
DECLARE @accountId INT;
SET @accountId = 8;
SELECT * FROM T_ACCOUNT WHERE ACCOUNTID = @accountId
SELECT * FROM T_RINGCENTRALTOKEN WHERE RINGCENTRALTOKENID IN (SELECT RINGCENTRALTOKENID FROM T_ACCOUNT WHERE ACCOUNTID = @accountId)
SELECT * FROM T_GOOGLEACCOUNT WHERE ACCOUNTID = @accountId
SELECT * FROM T_GOOGLETOKEN WHERE GOOGLETOKENID IN (SELECT GOOGLETOKENID FROM T_GOOGLEACCOUNT WHERE ACCOUNTID = @accountId)
SELECT * FROM T_BOXACCOUNT WHERE ACCOUNTID = @accountId
SELECT * FROM T_BOXTOKEN WHERE BOXTOKENID IN (SELECT BOXTOKENID FROM T_BOXACCOUNT WHERE ACCOUNTID = @accountId)
SELECT * FROM T_AMAZONACCOUNT WHERE ACCOUNTID = @accountId
SELECT * FROM T_AMAZONUSER WHERE AMAZONUSERID IN (SELECT AMAZONUSERID FROM T_AMAZONACCOUNT WHERE ACCOUNTID = @accountId)
SELECT * FROM T_TRANSFERBATCH WHERE ACCOUNTID = @accountId
SELECT * FROM T_TRANSFERBATCHLOG WHERE TRANSFERBATCHID IN (SELECT TRANSFERBATCHID FROM T_TRANSFERBATCH WHERE ACCOUNTID = @accountId)
SELECT * FROM T_TICKET WHERE TRANSFERBATCHID IN (SELECT TRANSFERBATCHID FROM T_TRANSFERBATCH WHERE ACCOUNTID = @accountId)
SELECT * FROM T_TICKETLOG WHERE TRANSFERBATCHID IN (SELECT TRANSFERBATCHID FROM T_TRANSFERBATCH WHERE ACCOUNTID = @accountId)
SELECT * FROM T_TICKETRAWDATA WHERE TRANSFERBATCHID IN (SELECT TRANSFERBATCHID FROM T_TRANSFERBATCH WHERE ACCOUNTID = @accountId)
SELECT * FROM T_TRANSFERRULE WHERE ACCOUNTID = @accountId
SELECT * FROM T_ANALYTICS WHERE ACCOUNTID = @accountId
SELECT * FROM T_RINGCENTRALCONTACT WHERE ACCOUNTID = @accountId


--GETTING THE TIME ELAPSED AND PUTTING IT IN THE RAW DATA
SELECT TOP 100 t_ticket.TICKETID,T_INDEXRAWDATA.RAWDATA,T_TICKETLOG.TICKETLOGSTARTDATE,T_TICKETLOG.TICKETLOGSTOPDATE,REPLACE(T_INDEXRAWDATA.RAWDATA,'"DefaultFileName":','"TimeElapsedForTransfer":' + CONVERT(VARCHAR(20),DATEDIFF(ms,T_TICKETLOG.TICKETLOGSTARTDATE,T_TICKETLOG.TICKETLOGSTOPDATE)) + ',"DefaultFileName":') FROM T_INDEX
INNER JOIN T_INDEXRAWDATA ON T_INDEX.INDEXRAWDATAID=T_INDEXRAWDATA.INDEXRAWDATAID
INNER JOIN T_TICKET ON T_INDEX.TICKETID=T_TICKET.TICKETID
INNER JOIN T_TICKETLOG ON T_TICKET.TICKETID = T_TICKETLOG.TICKETID
WHERE 
T_TICKETLOG.TICKETLOGSTARTDATE IS NOT NULL
AND T_TICKETLOG.TICKETLOGSTOPDATE IS NOT NULL
AND T_TICKETLOG.ERRORIND=0
AND T_TICKET.TICKETID <= 104466


--LOOK AT INDEXES
SELECT dbschemas.[name] as 'Schema', 
dbtables.[name] as 'Table',
dbindexes.[name] as 'Index',
indexstats.alloc_unit_type_desc,
indexstats.avg_fragmentation_in_percent,
indexstats.page_count
FROM sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, NULL) AS indexstats
INNER JOIN sys.tables dbtables on dbtables.[object_id] = indexstats.[object_id]
INNER JOIN sys.schemas dbschemas on dbtables.[schema_id] = dbschemas.[schema_id]
INNER JOIN sys.indexes AS dbindexes ON dbindexes.[object_id] = indexstats.[object_id]
AND indexstats.index_id = dbindexes.index_id
WHERE indexstats.database_id = DB_ID()
ORDER BY indexstats.avg_fragmentation_in_percent DESC
