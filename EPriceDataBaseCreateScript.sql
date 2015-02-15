USE master;
CREATE DATABASE e_price;
go

use e_price
go
CREATE TABLE UN1TCategory
(
	Id int NOT NULL,
	IdParent int NOT NULL,
	NAME nvarchar(250) NOT NULL,
	PRIMARY KEY (Id)
)

CREATE TABLE ProviderCategory
(
	Id nvarchar(100) NOT NULL,
	IdType int NOT NULL,
	IdParent nvarchar(100) NOT NULL,
	IdParentType int NOT NULL,
	Provider int NOT NULL,
	IdUN1TCategory int NOT NULL,
	CONSTRAINT FK_ProviderCategory_UN1TCategory FOREIGN KEY(IdUN1TCategory)
		REFERENCES UN1TCategory(Id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)

CREATE TABLE Product
(
	Id int IDENTITY NOT NULL,
	IdCategory int NOT NULL,
	Name nvarchar(1000) NOT NULL,
	Vendor nvarchar(200), 
	Brand nvarchar(200),
	PartNumber nvarchar(100) NOT NULL,
	Provider int NOT NULL,
	RecordDate datetime2(7) NOT NULL,
	PRIMARY KEY (Id),
	CONSTRAINT FK_Product_UN1TCategory FOREIGN KEY(IdCategory)
		REFERENCES UN1TCategory(Id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)

CREATE TABLE Stock
(
	IdProduct int NOT NULL,
	Currency int NOT NULL,
	Location int NOT NULL,
	Value int NOT NULL, 
	Price decimal(18,2) NOT NULL,
	Provider int NOT NULL,
	RecordDate datetime2(7) NOT NULL,
	CONSTRAINT FK_Stock_Product FOREIGN KEY(IdProduct)
		REFERENCES Product(Id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)

CREATE TABLE ProductProperty
(
	IdProduct int NOT NULL,
	Provider int NOT NULL,
	IdPropertyProvider varchar(100) NOT NULL,
	IdPropertyProviderType int NOT NULL,
	Name nvarchar(200) NOT NULL,
	Value nvarchar(500) NOT NULL,
	CONSTRAINT FK_ProductProperty_Product FOREIGN KEY(IdProduct)
		REFERENCES Product(Id)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
go

USE e_price
GO

CREATE INDEX i_partnumber on Product (PartNumber)
GO

USE e_price;
GO
CREATE PROCEDURE CreateUN1TCategory 
(
	@id int,
	@idParent int,
	@name nvarchar(250)
)
AS 
INSERT INTO UN1TCategory VALUES(@id, @idParent, @name)
GO

USE e_price;
GO
CREATE PROCEDURE UpdateUN1TCategory
(
	@id int,
	@name nvarchar(250)
)
AS 
UPDATE UN1TCategory SET Name = @name WHERE Id = @id
GO

USE e_price;
GO
CREATE PROCEDURE LoadAllUN1TCategory
AS 
SELECT *, (SELECT COUNT(*) FROM ProviderCategory WHERE IdUN1TCategory = UN1TCategory.Id) FROM UN1TCategory
GO

USE e_price;
GO
CREATE PROCEDURE DeleteUN1TCategory
(
	@id int
)
AS 
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION
	DELETE FROM UN1TCategory WHERE Id = @id
COMMIT TRANSACTION
GO

USE e_price;
GO
CREATE PROCEDURE CreateProviderCategory
(
	@id nvarchar(100),
	@idType int,
	@idParent nvarchar(100),
	@idParentType int,
	@provider int,
	@idUN1TCategory int
)
AS 
INSERT INTO ProviderCategory VALUES(@id, @idType, @idParent, @idParentType, @provider, @idUN1TCategory)
GO

USE e_price;
GO
CREATE PROCEDURE DeleteAllProviderCategories
AS 
DELETE FROM ProviderCategory
GO

USE e_price;
GO
CREATE PROCEDURE DeleteProvidersCategories
(
	@provider int
)
AS 
DELETE FROM ProviderCategory WHERE Provider = @provider
GO

USE e_price;
GO
CREATE PROCEDURE LoadProviderCategories
(
	@provider int
)
AS 
SELECT * FROM ProviderCategory WHERE Provider = @provider;
GO

USE e_price;
GO
CREATE PROCEDURE CreateProduct
(
	@idCategory int,
	@name nvarchar(1000),
	@vendor nvarchar(200), 
	@brand nvarchar(200),
	@partNumber nvarchar(100),
  @provider int,
  @recordDate datetime2(7)
)
AS
DECLARE @return int;
DECLARE @count int;
SET @count = (SELECT COUNT(*) FROM Product WHERE PartNumber = @partNumber);
IF @count = 0
BEGIN
	SET @return = 1;
	INSERT INTO Product VALUES(@idCategory, @name, @vendor, @brand, @partNumber, @provider, @recordDate)
END 
IF @count != 0
BEGIN
	SET @return = 0;
	IF @brand IS NOT NULL
	BEGIN
		DECLARE @currentBrand nvarchar(200);
		SET @currentBrand = (SELECT Brand FROM Product WHERE PartNumber = @partNumber);
		IF @currentBrand IS NULL
		BEGIN
			UPDATE Product SET Brand = @brand WHERE PartNumber = @partNumber
		END
	END
	IF @vendor IS NOT NULL
	BEGIN
		DECLARE @currentVendor nvarchar(200);
		SET @currentVendor = (SELECT Vendor FROM Product WHERE PartNumber = @partNumber);
		IF @currentVendor IS NULL
		BEGIN
			UPDATE Product SET Vendor = @vendor WHERE PartNumber = @partNumber
		END
	END
END
SELECT @return
GO

USE e_price;
GO
CREATE PROCEDURE LoadProductsForCategory
(
	@idCategory int
)
AS
SELECT * FROM Product WHERE IdCategory = @idCategory
GO

USE e_price;
GO
CREATE PROCEDURE SaveStock
(
	@partNumber nvarchar(100),
	@currency int,
	@location int,
	@value int,
	@price decimal(18,2),
	@provider int,
  @recordDate datetime2(7)
)
AS
DECLARE @id int;
SET @id = (SELECT Id FROM Product WHERE PartNumber = @partNumber);
IF @id IS NOT NULL
BEGIN
	INSERT INTO Stock VALUES(@id, @currency, @location, @value, @price, @provider, @recordDate)
END
GO

USE e_price;
GO
CREATE PROCEDURE DeleteStocksForProvider
(
	@provider int
)
AS
DELETE FROM Stock WHERE Provider = @provider
GO

USE e_price;
GO
CREATE PROCEDURE SaveProductProperty
(
	@partNumber nvarchar(100),
	@provider int,
	@idPropertyProvider varchar(100),
	@idPropertyProviderType int,
	@name nvarchar(200),
	@value nvarchar(500)
)
AS
DECLARE @id int;
SET @id = (SELECT Id FROM Product WHERE PartNumber = @partNumber);
IF @id IS NOT NULL
BEGIN
	DECLARE @count int;
	SET @count = (SELECT COUNT(*) FROM ProductProperty WHERE IdProduct = @id AND Provider = @provider AND IdPropertyProvider = @idPropertyProvider);
	IF @count = 0
	BEGIN
		INSERT INTO ProductProperty VALUES(@id, @provider, @idPropertyProvider, @idPropertyProviderType, @name, @value)
	END
END
GO

USE e_price
GO
CREATE PROCEDURE LoadStocksForProducts
(
	@categoryId int
)
AS
SELECT * FROM Stock WHERE IdProduct IN (SELECT Id FROM Product WHERE IdCategory = @categoryId)
GO

USE e_price
GO
CREATE PROCEDURE LoadProperiesForProduct
(
	@idProduct int
)
AS
SELECT * FROM ProductProperty WHERE IdProduct = @idProduct
GO