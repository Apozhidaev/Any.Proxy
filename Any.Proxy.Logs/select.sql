USE [PLogs]
GO

SELECT [Id]
      ,[Time]
      ,[Summary]
      ,[Description]
      ,[Type]
      ,[Context]
  FROM [dbo].[SysEvents]
  where
  --[Type] = 1
  --[Summary] = 'vk.com'
  [Context] = '3762ce47-2b09-482c-973d-aa96d4337a91'
  order by [Time]
GO


