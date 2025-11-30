-- Funkcja sumuj¹ca wartoœæ zamówienia
IF OBJECT_ID('dbo.ufn_OrderTotal','FN') IS NOT NULL
    DROP FUNCTION dbo.ufn_OrderTotal;
GO
CREATE FUNCTION dbo.ufn_OrderTotal(@OrderId INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @total DECIMAL(18,2);
    SELECT @total = ISNULL(SUM(oi.Quantity * oi.PriceWhenOrdered), 0)
    FROM OrderedItems oi
    WHERE oi.OrderId = @OrderId;
    RETURN @total;
END;
GO

-- Procedura dodaj¹ca sprzêt
IF OBJECT_ID('dbo.spAddEquipment','P') IS NOT NULL
    DROP PROCEDURE dbo.spAddEquipment;
GO
CREATE PROCEDURE dbo.spAddEquipment
    @Type INT,
    @Size INT,
    @Price DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO Equipment (Type, Size, Is_In_Werehouse, Price, Is_Reserved)
    VALUES (@Type, @Size, 1, @Price, 0);

    SELECT SCOPE_IDENTITY() AS NewEquipmentId;
END;
GO

-- Procedura tworz¹ca zamówienie
IF OBJECT_ID('dbo.spPlaceOrder','P') IS NOT NULL
    DROP PROCEDURE dbo.spPlaceOrder;
GO
CREATE PROCEDURE dbo.spPlaceOrder
    @UserId NVARCHAR(450),
    @RentalInfoId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OrderId INT;

    INSERT INTO Orders (Rented_Items, OrderDate, Price, Date_Of_submission, Was_It_Returned, UserId, RentalInfoId)
    VALUES (N'', SYSUTCDATETIME(), 0, CONVERT(date, SYSUTCDATETIME()), 0, @UserId, @RentalInfoId);

    SET @OrderId = SCOPE_IDENTITY();

    UPDATE Orders SET Price = dbo.ufn_OrderTotal(@OrderId) WHERE Id = @OrderId;

    SELECT @OrderId AS OrderId;
END;
GO

-- Trigger ustawiaj¹cy rezerwacjê po dodaniu pozycji
IF OBJECT_ID('dbo.trg_OrderedItems_AfterInsert','TR') IS NOT NULL
    DROP TRIGGER dbo.trg_OrderedItems_AfterInsert;
GO
CREATE TRIGGER dbo.trg_OrderedItems_AfterInsert
ON OrderedItems
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE e
        SET e.Is_Reserved = 1
    FROM Equipment e
    INNER JOIN inserted i ON i.EquipmentId = e.Id;
END;
GO
