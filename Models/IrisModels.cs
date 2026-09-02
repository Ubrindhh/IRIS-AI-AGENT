using System.Text.Json.Serialization;

namespace IrisAI.Agent.Models;

public sealed class IrisCustomerSearchResponse
{
	[JsonPropertyName("values")]
	public List<IrisCustomer> Values { get; set; } = new();

	[JsonPropertyName("totalSize")]
	public int TotalSize { get; set; }
}

public sealed class IrisCustomer
{
	[JsonPropertyName("customerId")]
	public string CustomerId { get; set; } = string.Empty;

	[JsonPropertyName("customerType")]
	public string CustomerType { get; set; } = string.Empty;

	[JsonPropertyName("customerDescription")]
	public string CustomerDescription { get; set; } = string.Empty;

	[JsonPropertyName("customerTitle")]
	public string CustomerTitle { get; set; } = string.Empty;

	[JsonPropertyName("firstName")]
	public string FirstName { get; set; } = string.Empty;

	[JsonPropertyName("middleName")]
	public string MiddleName { get; set; } = string.Empty;

	[JsonPropertyName("lastName")]
	public string LastName { get; set; } = string.Empty;

	[JsonPropertyName("gender")]
	public string Gender { get; set; } = string.Empty;

	[JsonPropertyName("dateOfBirth")]
	public string DateOfBirth { get; set; } = string.Empty;

	[JsonPropertyName("mobileNumber")]
	public string MobileNumber { get; set; } = string.Empty;

	[JsonPropertyName("emailAddress")]
	public string EmailAddress { get; set; } = string.Empty;

	[JsonPropertyName("nationalId")]
	public string NationalId { get; set; } = string.Empty;

	public string FullName =>
		string.Join(
			" ",
			new[]
			{
				FirstName,
				MiddleName,
				LastName
			}
			.Where(x => !string.IsNullOrWhiteSpace(x)));
}


public sealed class IrisProductsResponse
{
	[JsonPropertyName("totalSize")]
	public int TotalSize { get; set; }

	[JsonPropertyName("pageSize")]
	public int PageSize { get; set; }

	[JsonPropertyName("pageNumber")]
	public int PageNumber { get; set; }

	[JsonPropertyName("items")]
	public List<IrisProduct> Items { get; set; } = new();
}

public sealed class IrisProduct
{
	[JsonPropertyName("productId")]
	public string ProductId { get; set; } = string.Empty;

	[JsonPropertyName("productCode")]
	public string ProductCode { get; set; } = string.Empty;

	[JsonPropertyName("productName")]
	public string ProductName { get; set; } = string.Empty;

	[JsonPropertyName("brand")]
	public string Brand { get; set; } = string.Empty;

	[JsonPropertyName("type")]
	public string Type { get; set; } = string.Empty;

	[JsonPropertyName("currency")]
	public string Currency { get; set; } = string.Empty;

	[JsonPropertyName("BIN")]
	public string Bin { get; set; } = string.Empty;

	[JsonPropertyName("formFactor")]
	public string FormFactor { get; set; } = string.Empty;

	[JsonPropertyName("primaryProduct")]
	public string PrimaryProduct { get; set; } = string.Empty;
}


public sealed class IrisAccountsResponse
{
	[JsonPropertyName("values")]
	public List<IrisAccount> Values { get; set; } = new();

	[JsonPropertyName("totalSize")]
	public int TotalSize { get; set; }
}

public sealed class IrisAccount
{
	[JsonPropertyName("accountBranch")]
	public string AccountBranch { get; set; } = string.Empty;

	[JsonPropertyName("accountBranchCode")]
	public string AccountBranchCode { get; set; } = string.Empty;

	[JsonPropertyName("accountCurrency")]
	public string AccountCurrency { get; set; } = string.Empty;

	[JsonPropertyName("accountCurrencyId")]
	public string AccountCurrencyId { get; set; } = string.Empty;

	[JsonPropertyName("accountTitle")]
	public string AccountTitle { get; set; } = string.Empty;

	[JsonPropertyName("accountNumber")]
	public string AccountNumber { get; set; } = string.Empty;

	[JsonPropertyName("accountStatusCode")]
	public string AccountStatusCode { get; set; } = string.Empty;

	[JsonPropertyName("accountStatus")]
	public string AccountStatus { get; set; } = string.Empty;

	[JsonPropertyName("accountType")]
	public string AccountType { get; set; } = string.Empty;

	[JsonPropertyName("accountTypeId")]
	public string AccountTypeId { get; set; } = string.Empty;

	[JsonPropertyName("accountCategory")]
	public string AccountCategory { get; set; } = string.Empty;

	[JsonPropertyName("accountCategoryCode")]
	public string AccountCategoryCode { get; set; } = string.Empty;

	[JsonPropertyName("availableBalance")]
	public string AvailableBalance { get; set; } = string.Empty;

	[JsonPropertyName("actualBalance")]
	public string ActualBalance { get; set; } = string.Empty;

	[JsonPropertyName("productID")]
	public string ProductId { get; set; } = string.Empty;
}


public sealed class CreateDebitCardRequest
{
	[JsonPropertyName("ProductCode")]
	public string ProductCode { get; set; } = string.Empty;

	[JsonPropertyName("NameonCard")]
	public string NameonCard { get; set; } = string.Empty;

	[JsonPropertyName("DeliveryBranch")]
	public string DeliveryBranch { get; set; } = string.Empty;

	[JsonPropertyName("AccountNumber")]
	public string AccountNumber { get; set; } = string.Empty;

	[JsonPropertyName("AccountType")]
	public string AccountType { get; set; } = string.Empty;

	[JsonPropertyName("CurrencyCode")]
	public string CurrencyCode { get; set; } = string.Empty;
}


public sealed class IrisCreateCardResponse
{
	[JsonPropertyName("customerId")]
	public string CustomerId { get; set; } = string.Empty;

	[JsonPropertyName("cardNumber")]
	public string CardNumber { get; set; } = string.Empty;

	[JsonPropertyName("cardId")]
	public string CardId { get; set; } = string.Empty;

	[JsonPropertyName("nameOnCard")]
	public string NameOnCard { get; set; } = string.Empty;

	[JsonPropertyName("cardProduct")]
	public string CardProduct { get; set; } = string.Empty;

	[JsonPropertyName("cardProductId")]
	public string CardProductId { get; set; } = string.Empty;

	[JsonPropertyName("productCategory")]
	public string ProductCategory { get; set; } = string.Empty;

	[JsonPropertyName("cardProductCode")]
	public string CardProductCode { get; set; } = string.Empty;

	[JsonPropertyName("cardType")]
	public string CardType { get; set; } = string.Empty;

	[JsonPropertyName("cardCategory")]
	public string CardCategory { get; set; } = string.Empty;

	[JsonPropertyName("cardStatusCode")]
	public string CardStatusCode { get; set; } = string.Empty;

	[JsonPropertyName("cardStatus")]
	public string CardStatus { get; set; } = string.Empty;

	[JsonPropertyName("retriesLeft")]
	public string RetriesLeft { get; set; } = string.Empty;

	[JsonPropertyName("maxRetries")]
	public string MaxRetries { get; set; } = string.Empty;

	[JsonPropertyName("relationshipId")]
	public string RelationshipId { get; set; } = string.Empty;

	[JsonPropertyName("address1")]
	public string Address1 { get; set; } = string.Empty;

	[JsonPropertyName("address2")]
	public string Address2 { get; set; } = string.Empty;

	[JsonPropertyName("statusReason")]
	public string StatusReason { get; set; } = string.Empty;

	[JsonPropertyName("expiryDate")]
	public string ExpiryDate { get; set; } = string.Empty;

	[JsonPropertyName("cardExistence")]
	public string CardExistence { get; set; } = string.Empty;
}


// GET /api/v1/customers/{cnic}/Cards  and  GET /api/v1/Cards/{cardId}
public sealed class IrisCardsResponse
{
	[JsonPropertyName("values")]
	public List<IrisCard> Values { get; set; } = new();

	[JsonPropertyName("totalSize")]
	public int TotalSize { get; set; }
}

public sealed class IrisCard
{
	[JsonPropertyName("cardId")]
	public string CardId { get; set; } = string.Empty;

	[JsonPropertyName("cardNumber")]
	public string CardNumber { get; set; } = string.Empty;

	[JsonPropertyName("nameOnCard")]
	public string NameOnCard { get; set; } = string.Empty;

	[JsonPropertyName("cardProduct")]
	public string CardProduct { get; set; } = string.Empty;

	[JsonPropertyName("cardProductCode")]
	public string CardProductCode { get; set; } = string.Empty;

	[JsonPropertyName("cardType")]
	public string CardType { get; set; } = string.Empty;

	[JsonPropertyName("cardCategory")]
	public string CardCategory { get; set; } = string.Empty;

	[JsonPropertyName("cardStatusCode")]
	public string CardStatusCode { get; set; } = string.Empty;

	[JsonPropertyName("cardStatus")]
	public string CardStatus { get; set; } = string.Empty;

	[JsonPropertyName("statusReason")]
	public string StatusReason { get; set; } = string.Empty;

	[JsonPropertyName("expiryDate")]
	public string ExpiryDate { get; set; } = string.Empty;

	[JsonPropertyName("customerId")]
	public string CustomerId { get; set; } = string.Empty;

	[JsonPropertyName("primaryCard")]
	public string PrimaryCard { get; set; } = string.Empty;

	[JsonPropertyName("cardCreationDate")]
	public string CardCreationDate { get; set; } = string.Empty;
}


// GET /api/v1/branches
public sealed class IrisBranchesResponse
{
	[JsonPropertyName("values")]
	public List<IrisBranch> Values { get; set; } = new();

	[JsonPropertyName("totalSize")]
	public int TotalSize { get; set; }
}

public sealed class IrisBranch
{
	[JsonPropertyName("branchId")]
	public string BranchId { get; set; } = string.Empty;

	[JsonPropertyName("branchName")]
	public string BranchName { get; set; } = string.Empty;
}


// GET /api/v1/currencies
public sealed class IrisCurrenciesResponse
{
	[JsonPropertyName("values")]
	public List<IrisCurrency> Values { get; set; } = new();

	[JsonPropertyName("totalSize")]
	public int TotalSize { get; set; }
}

public sealed class IrisCurrency
{
	[JsonPropertyName("code")]
	public string Code { get; set; } = string.Empty;

	[JsonPropertyName("symbol")]
	public string Symbol { get; set; } = string.Empty;

	[JsonPropertyName("description")]
	public string Description { get; set; } = string.Empty;

	[JsonPropertyName("decimalPlace")]
	public string DecimalPlace { get; set; } = string.Empty;

	[JsonPropertyName("baseCurrency")]
	public bool BaseCurrency { get; set; }
}