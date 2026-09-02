using System.Text.Json;
using IrisAI.Agent.Models;
using IrisAI.Agent.Services;

namespace IrisAI.Agent.Tools;

public sealed class IrisTools
{
	private readonly IrisApiClient _irisApi;

	public IrisTools(IrisApiClient irisApi)
	{
		_irisApi = irisApi;
	}

	public object[] GetDefinitions() =>
	[
		new
		{
			type = "function",
			function = new
			{
				name = "get_customer",
				description =
					"Find an existing IRIS customer using the National ID.",
				parameters = new
				{
					type = "object",
					properties = new
					{
						nationalId = new
						{
							type = "string",
							description =
								"Customer National ID."
						}
					},
					required = new[] { "nationalId" }
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "get_products",
				description =
					"Get the available card products from IRIS.",
				parameters = new
				{
					type = "object",
					properties = new { }
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "get_customer_accounts",
				description =
					"Get accounts belonging to an existing customer using their customer ID.",
				parameters = new
				{
					type = "object",
					properties = new
					{
						customerId = new
						{
							type = "string",
							description = "Existing IRIS customer ID."
						}
					},
					required = new[] { "customerId" }
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "create_card",
				description =
					"Prepare or create a debit card in IRIS. " +
					"Call with the complete pending request before asking for confirmation. " +
					"The card is created only after the user explicitly confirms that request.",
				parameters = new
				{
					type = "object",
					properties = new
					{
						customerId = new { type = "string" },
						productCode = new { type = "string" },
						nameOnCard = new { type = "string" },
						deliveryBranch = new { type = "string" },
						accountNumber = new { type = "string" },
						accountType = new { type = "string" },
						currencyCode = new { type = "string" }
					},
					required = new[]
					{
						"customerId",
						"productCode",
						"nameOnCard",
						"deliveryBranch",
						"accountNumber",
						"accountType",
						"currencyCode"
					}
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "get_customer_cards",
				description =
					"List the debit cards already issued to the current customer.",
				parameters = new
				{
					type = "object",
					properties = new { }
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "get_card",
				description =
					"Get details of a single card by its IRIS card id.",
				parameters = new
				{
					type = "object",
					properties = new
					{
						cardId = new
						{
							type = "string",
							description = "IRIS card id."
						}
					},
					required = new[] { "cardId" }
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "get_branches",
				description =
					"Get the list of bank branches (name and branch code).",
				parameters = new
				{
					type = "object",
					properties = new { }
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "get_currencies",
				description =
					"Get the list of supported currencies.",
				parameters = new
				{
					type = "object",
					properties = new { }
				}
			}
		},

		new
		{
			type = "function",
			function = new
			{
				name = "search_customers",
				description =
					"Find customers by name, mobile number, or email address " +
					"when the National ID (CNIC) is not known.",
				parameters = new
				{
					type = "object",
					properties = new
					{
						name = new { type = "string" },
						mobile = new { type = "string" },
						email = new { type = "string" }
					}
				}
			}
		}
	];

	public async Task<object> ExecuteAsync(
		string name,
		string argumentsJson,
		AgentSession session,
		CancellationToken cancellationToken)
	{
		using var json =
			JsonDocument.Parse(argumentsJson);

		var root =
			json.RootElement;

		switch (name)
		{
			case "get_customer":
				return await GetCustomerAsync(
					root,
					session,
					cancellationToken);

			case "get_products":
				return await GetProductsAsync(
					session,
					cancellationToken);

			case "get_customer_accounts":
				return await GetCustomerAccountsAsync(
					root,
					session,
					cancellationToken);

			case "create_card":
				return await CreateCardAsync(
					root,
					session,
					cancellationToken);

			case "get_customer_cards":
				return await GetCustomerCardsAsync(
					session,
					cancellationToken);

			case "get_card":
				return await GetCardAsync(
					root,
					cancellationToken);

			case "get_branches":
				return await GetBranchesAsync(
					cancellationToken);

			case "get_currencies":
				return await GetCurrenciesAsync(
					cancellationToken);

			case "search_customers":
				return await SearchCustomersAsync(
					root,
					cancellationToken);

			default:

				return new
				{
					success = false,
					error =
						$"Unknown tool: {name}"
				};
		}
	}

	private async Task<object> GetCustomerAsync(
		JsonElement root,
		AgentSession session,
		CancellationToken ct)
	{
		var nationalId =
			GetRequired(root, "nationalId");

		var customer =
			await _irisApi.GetCustomerAsync(
				nationalId,
				ct);

		if (customer == null)
		{
			return new
			{
				success = false,
				found = false,
				nationalId,
				message =
					"No customer was found."
			};
		}

		session.PendingCard.NationalId = nationalId;
		session.PendingCard.CustomerId = customer.CustomerId;
		session.PendingCard.CustomerName = customer.FullName;

		return new
		{
			success = true,
			found = true,
			nationalId,
			customerId =
				customer.CustomerId,
			customerName =
				customer.FullName,
			customerType =
				customer.CustomerDescription
		};
	}

	private async Task<object> GetProductsAsync(
		AgentSession session,
		CancellationToken ct)
	{
		var products =
			await _irisApi.GetProductsAsync(ct);

		var latestUserMessage = session.Messages
			.LastOrDefault(x => x.Role == "user")
			?.Content;

		var selectedProduct = FindProductInMessage(
			products,
			latestUserMessage);

		if (selectedProduct != null)
		{
			session.PendingCard.ProductCode = selectedProduct.ProductCode;
			session.PendingCard.ProductName = selectedProduct.ProductName;
		}

		return new
		{
			success = true,
			totalSize = products.Count,
			products = products
				.Select(x => new
				{
					productId = x.ProductId,
					productCode = x.ProductCode,
					productName = x.ProductName,
					type = x.Type,
					currency = x.Currency,
					formFactor = x.FormFactor
				})
				.ToList()
		};
	}

	private async Task<object> GetCustomerAccountsAsync(
	JsonElement root,
	AgentSession session,
	CancellationToken ct)
	{
		// IRIS looks accounts up by National ID (CNIC). Prefer the CNIC
		// already resolved for this session; fall back to the argument.
		var lookupId =
			!string.IsNullOrWhiteSpace(session.PendingCard.NationalId)
				? session.PendingCard.NationalId!
				: GetRequired(root, "customerId");

		var customerId = lookupId;

		var accounts =
			await _irisApi.GetCustomerAccountsAsync(
				lookupId,
				ct);

		var latestUserMessage = session.Messages
			.LastOrDefault(x => x.Role == "user")
			?.Content;

		var selectedAccount = FindAccountInMessage(
			accounts,
			latestUserMessage) ??
			(accounts.Count == 1 ? accounts[0] : null);

		if (selectedAccount != null)
		{
			session.PendingCard.AccountNumber = selectedAccount.AccountNumber;
			session.PendingCard.AccountType = selectedAccount.AccountType;
			session.PendingCard.AccountTypeId = selectedAccount.AccountTypeId;
			session.PendingCard.CurrencyCode = selectedAccount.AccountCurrencyId;
			session.PendingCard.AccountBranch = selectedAccount.AccountBranch;
			session.PendingCard.AccountBranchCode = selectedAccount.AccountBranchCode;

			// Default the delivery branch to the account's branch code
			// unless the user has already given one explicitly.
			if (string.IsNullOrWhiteSpace(session.PendingCard.DeliveryBranch) &&
				!string.IsNullOrWhiteSpace(selectedAccount.AccountBranchCode))
			{
				session.PendingCard.DeliveryBranch = selectedAccount.AccountBranchCode;
			}
		}

		return new
		{
			success = true,
			customerId,
			totalSize = accounts.Count,
			accounts = accounts
				.Select(x => new
				{
					accountNumber = x.AccountNumber,
					accountTitle = x.AccountTitle,
					accountType = x.AccountType,
					accountTypeId = x.AccountTypeId,
					currency = x.AccountCurrency,
					currencyCode = x.AccountCurrencyId,
					status = x.AccountStatus,
					branch = x.AccountBranch,
					branchCode = x.AccountBranchCode
				})
				.ToList()
		};
	}

	private async Task<object> GetCustomerCardsAsync(
		AgentSession session,
		CancellationToken ct)
	{
		var nationalId = session.PendingCard.NationalId;

		if (string.IsNullOrWhiteSpace(nationalId))
		{
			return new
			{
				success = false,
				error = "No customer has been identified yet."
			};
		}

		var cards =
			await _irisApi.GetCustomerCardsAsync(nationalId, ct);

		return new
		{
			success = true,
			nationalId,
			totalSize = cards.Count,
			cards = cards
				.Select(x => new
				{
					cardId = x.CardId,
					cardNumber = x.CardNumber,
					nameOnCard = x.NameOnCard,
					product = x.CardProduct,
					productCode = x.CardProductCode,
					cardType = x.CardType,
					status = x.CardStatus,
					statusCode = x.CardStatusCode,
					expiryDate = x.ExpiryDate
				})
				.ToList()
		};
	}

	private async Task<object> GetCardAsync(
		JsonElement root,
		CancellationToken ct)
	{
		var cardId = GetRequired(root, "cardId");

		var card = await _irisApi.GetCardAsync(cardId, ct);

		if (card == null)
		{
			return new
			{
				success = false,
				found = false,
				cardId,
				message = "No card was found."
			};
		}

		return new
		{
			success = true,
			found = true,
			cardId = card.CardId,
			cardNumber = card.CardNumber,
			nameOnCard = card.NameOnCard,
			product = card.CardProduct,
			productCode = card.CardProductCode,
			cardType = card.CardType,
			status = card.CardStatus,
			statusCode = card.CardStatusCode,
			statusReason = card.StatusReason,
			expiryDate = card.ExpiryDate,
			customerId = card.CustomerId
		};
	}

	private async Task<object> GetBranchesAsync(
		CancellationToken ct)
	{
		var branches = await _irisApi.GetBranchesAsync(ct);

		return new
		{
			success = true,
			totalSize = branches.Count,
			branches = branches
				.Select(x => new
				{
					branchCode = x.BranchId,
					branchName = x.BranchName
				})
				.ToList()
		};
	}

	private async Task<object> GetCurrenciesAsync(
		CancellationToken ct)
	{
		var currencies = await _irisApi.GetCurrenciesAsync(ct);

		return new
		{
			success = true,
			totalSize = currencies.Count,
			currencies = currencies
				.Take(40)
				.Select(x => new
				{
					code = x.Code,
					symbol = x.Symbol,
					name = x.Description,
					baseCurrency = x.BaseCurrency
				})
				.ToList()
		};
	}

	private async Task<object> SearchCustomersAsync(
		JsonElement root,
		CancellationToken ct)
	{
		var name = GetOptional(root, "name");
		var mobile = GetOptional(root, "mobile");
		var email = GetOptional(root, "email");

		if (string.IsNullOrWhiteSpace(name) &&
			string.IsNullOrWhiteSpace(mobile) &&
			string.IsNullOrWhiteSpace(email))
		{
			return new
			{
				success = false,
				error = "Provide a name, mobile number, or email to search."
			};
		}

		var customers =
			await _irisApi.SearchCustomersAsync(name, mobile, email, ct);

		return new
		{
			success = true,
			totalSize = customers.Count,
			customers = customers
				.Select(x => new
				{
					customerId = x.CustomerId,
					customerName = x.FullName,
					nationalId = x.NationalId,
					mobileNumber = x.MobileNumber,
					emailAddress = x.EmailAddress,
					customerType = x.CustomerDescription
				})
				.ToList()
		};
	}

	private Task<object> CreateCardAsync(
		JsonElement root,
		AgentSession session,
		CancellationToken ct)
	{
		var customerId = GetPendingOrRequired(
			session.PendingCard.CustomerId,
			root,
			"customerId");

		var productCode = GetPendingOrRequired(
			session.PendingCard.ProductCode,
			root,
			"productCode");

		var nameOnCard = GetPendingOrRequired(
			session.PendingCard.NameOnCard,
			root,
			"nameOnCard");

		var deliveryBranch = GetPendingOrRequired(
			session.PendingCard.DeliveryBranch,
			root,
			"deliveryBranch");

		var accountNumber = GetPendingOrRequired(
			session.PendingCard.AccountNumber,
			root,
			"accountNumber");

		var accountType = GetPendingOrRequired(
			session.PendingCard.AccountType,
			root,
			"accountType");

		var currencyCode = GetPendingOrRequired(
			session.PendingCard.CurrencyCode,
			root,
			"currencyCode");

		if (!session.PendingCard.IsReadyToCreate ||
			!session.PendingCard.Matches(
				customerId,
				productCode,
				accountNumber,
				accountType,
				currencyCode,
				nameOnCard,
				deliveryBranch))
		{
			session.PendingCard.Set(
				customerId,
				productCode,
				accountNumber,
				accountType,
				currencyCode,
				nameOnCard,
				deliveryBranch);

			session.ConfirmationReceived = false;

			return Task.FromResult<object>(new
			{
				success = false,
				blocked = true,
				reason =
					"Please show the pending card request and obtain explicit confirmation before creation."
			});
		}

		return Task.FromResult<object>(new
		{
			success = false,
			blocked = true,
			reason =
				"Explicit user confirmation is required before card creation."
		});
	}

	public async Task<IrisCreateCardResponse?> CreateConfirmedCardAsync(
		AgentSession session,
		CancellationToken ct)
	{
		if (!session.ConfirmationReceived ||
			!session.PendingCard.IsReadyToCreate)
		{
			throw new InvalidOperationException(
				"A complete pending card request and explicit confirmation are required.");
		}

		var pending = session.PendingCard;

		// IRIS expects the numeric account-type id (e.g. "10"), not the
		// display name (e.g. "Current"), both as the AccountType field and
		// inside the composed AccountNumber.
		var accountTypeId =
			!string.IsNullOrWhiteSpace(pending.AccountTypeId)
				? pending.AccountTypeId!
				: pending.AccountType!;

		var request = new CreateDebitCardRequest
		{
			ProductCode = pending.ProductCode!,
			NameonCard = pending.NameOnCard!,
			DeliveryBranch = pending.DeliveryBranch!,
			AccountNumber = BuildFormattedAccountNumber(
				pending.AccountNumber!,
				accountTypeId,
				pending.CurrencyCode!),
			AccountType = accountTypeId,
			CurrencyCode = pending.CurrencyCode!
		};

		try
		{
			var response = await _irisApi.CreateCardAsync(
				pending.CustomerId!,
				request,
				ct);

			session.ConfirmationReceived = false;

			// Success: keep the identified customer so the employee can,
			// e.g., immediately list that customer's cards; only clear the
			// card that was just created.
			session.PendingCard.ResetCardSelections();

			return response;
		}
		catch
		{
			// Failure: keep the pending card so the employee can correct a
			// field and confirm again.
			session.ConfirmationReceived = false;
			throw;
		}
	}

	private static string GetRequired(
		JsonElement root,
		string name)
	{
		if (!root.TryGetProperty(
				name,
				out var value)
			||
			value.ValueKind !=
				JsonValueKind.String)
		{
			throw new InvalidOperationException(
				$"Tool argument '{name}' is required.");
		}

		var text =
			value.GetString();

		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException(
				$"Tool argument '{name}' is required.");
		}

		return text.Trim();
	}

	private static string GetPendingOrRequired(
		string? pendingValue,
		JsonElement root,
		string name)
		=> !string.IsNullOrWhiteSpace(pendingValue)
			? pendingValue
			: GetRequired(root, name);

	private static string? GetOptional(
		JsonElement root,
		string name)
		=> root.TryGetProperty(name, out var value) &&
		   value.ValueKind == JsonValueKind.String
			? value.GetString()?.Trim()
			: null;

	private static IrisProduct? FindProductInMessage(
		IEnumerable<IrisProduct> products,
		string? message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return null;
		}

		var list = products.ToList();

		var tokens = message.Split(
			new[] { ' ', ',', ';', '.', ':', '\n', '\r', '\t', '(', ')', '"', '\'' },
			StringSplitOptions.RemoveEmptyEntries);

		// 1. An exact product code mentioned as its own token wins.
		var byCode = list.FirstOrDefault(product =>
			!string.IsNullOrWhiteSpace(product.ProductCode) &&
			tokens.Any(t => t.Equals(
				product.ProductCode,
				StringComparison.OrdinalIgnoreCase)));

		if (byCode != null)
		{
			return byCode;
		}

		// 2. Otherwise match by product name, preferring the longest
		//    (most specific) name contained in the message so that
		//    "Visa Product" is chosen over "Visa".
		return list
			.Where(product =>
				!string.IsNullOrWhiteSpace(product.ProductName) &&
				message.Contains(
					product.ProductName,
					StringComparison.OrdinalIgnoreCase))
			.OrderByDescending(product => product.ProductName.Length)
			.FirstOrDefault();
	}

	private static IrisAccount? FindAccountInMessage(
		IEnumerable<IrisAccount> accounts,
		string? message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return null;
		}

		return accounts.FirstOrDefault(account =>
			!string.IsNullOrWhiteSpace(account.AccountNumber) &&
			message.Contains(
				account.AccountNumber,
				StringComparison.OrdinalIgnoreCase));
	}

	private static string BuildFormattedAccountNumber(
	string accountNumber,
	string accountType,
	string currencyCode)
	{
		// If the account number is already formatted,
		// avoid appending the values again.
		if (accountNumber.EndsWith(
				$"-{accountType}-{currencyCode}",
				StringComparison.OrdinalIgnoreCase))
		{
			return accountNumber;
		}

		return
			$"{accountNumber.Trim()}-" +
			$"{accountType.Trim()}-" +
			$"{currencyCode.Trim()}";
	}
}
