USE [Producer]
GO

/****** Object:  Table [dbo].[Items]    Script Date: 10/04/2025 17:31:06 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Items](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Value] [nvarchar](6) NULL
) ON [PRIMARY]
GO


