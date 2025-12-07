// Bookly App - Main JavaScript File
if (!window.__booklyAppLoaded) {
  window.__booklyAppLoaded = true;

  // API Configuration
  const API_BASE = "https://localhost:7104";

  // State Management
  const state = {
    token: localStorage.getItem("token") || "",
    userId: localStorage.getItem("userId") ? Number(localStorage.getItem("userId")) : null,
    role: localStorage.getItem("role") || "customer",
    fullName: localStorage.getItem("fullName") || "",
    email: localStorage.getItem("email") || "",
  };

  window.state = state;

  // ============================================
  // UTILITY FUNCTIONS
  // ============================================
  // Format price to VNĐ
  const formatVND = (price) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(price);
  };

  window.formatVND = formatVND;

  // Get active promotions and calculate discount
  let cachedPromotions = null;
  const getPromotions = async () => {
    if (cachedPromotions) return cachedPromotions;
    try {
      const promotions = await apiRequest("/api/admin/promotions");
      const activePromotions = (Array.isArray(promotions) ? promotions : []).filter(p => {
        const now = new Date();
        const startDate = new Date(p.startDate || p.StartDate);
        const endDate = new Date(p.endDate || p.EndDate);
        return (p.isActive || p.IsActive) && now >= startDate && now <= endDate;
      });
      cachedPromotions = activePromotions;
      return activePromotions;
    } catch (err) {
      console.error("Get promotions failed:", err);
      return [];
    }
  };

  // Calculate discounted price for a book
  const calculateDiscountedPrice = async (bookId, originalPrice) => {
    if (!bookId || !originalPrice) {
      return {
        originalPrice: originalPrice || 0,
        discountedPrice: originalPrice || 0,
        discount: 0,
        discountType: null,
        discountValue: 0,
        hasDiscount: false
      };
    }

    const promotions = await getPromotions();
    let bestDiscount = 0;
    let discountType = null;
    let discountValue = 0;

    for (const promo of promotions) {
      const items = promo.promotionItems || promo.PromotionItems || [];
      // Try to find item by bookId (handle both number and string comparison)
      const item = items.find(pi => {
        const piBookId = pi.bookId || pi.BookId;
        return Number(piBookId) === Number(bookId);
      });
      
      if (item) {
        const promoType = promo.promotionType || promo.PromotionType;
        const discountVal = promo.discountValue || promo.DiscountValue || 0;
        const specificDiscount = item.specificDiscount || item.SpecificDiscount;
        
        let discount = 0;
        if (specificDiscount != null && specificDiscount !== undefined && specificDiscount !== 0) {
          // Use specific discount if available (in VND)
          discount = Number(specificDiscount);
        } else if (promoType === 'percentage') {
          // Calculate percentage discount
          discount = (originalPrice * Number(discountVal)) / 100;
        } else if (promoType === 'fixed') {
          // Fixed amount discount (in VND)
          discount = Number(discountVal);
        }

        if (discount > bestDiscount) {
          bestDiscount = discount;
          discountType = promoType;
          discountValue = specificDiscount != null && specificDiscount !== undefined && specificDiscount !== 0 
            ? specificDiscount 
            : discountVal;
        }
      }
    }

    const finalPrice = Math.max(0, originalPrice - bestDiscount);
    return {
      originalPrice,
      discountedPrice: finalPrice,
      discount: bestDiscount,
      discountType,
      discountValue,
      hasDiscount: bestDiscount > 0
    };
  };

  window.calculateDiscountedPrice = calculateDiscountedPrice;

  // ============================================
  // API REQUEST FUNCTION
  // ============================================
  const apiRequest = async (path, options = {}) => {
    try {
      const url = `${API_BASE}${path}`;
      const method = options.method || 'GET';
      
      const headers = {
        "Accept": "application/json",
        ...(state.token ? { Authorization: `Bearer ${state.token}` } : {}),
        ...options.headers,
      };
      
      if (method !== 'GET' && method !== 'HEAD' && options.body) {
        headers["Content-Type"] = "application/json";
      }

      const res = await fetch(url, {
        method: method,
        headers: headers,
        ...options,
      });

      if (res.status === 204) return null;

      if (res.ok) {
        const contentType = res.headers.get("content-type");
        if (contentType && contentType.includes("application/json")) {
          return await res.json();
        } else {
          const text = await res.text();
          return text ? JSON.parse(text) : null;
        }
      } else {
        const errorText = await res.text();
        let errorData;
        try {
          errorData = JSON.parse(errorText);
        } catch {
          errorData = { message: errorText || res.statusText };
        }
        throw new Error(errorData.message || errorData.error || "Request failed");
      }
    } catch (err) {
      console.error(`[API] Error for ${path}:`, err);
      throw err;
    }
  };

  window.apiRequest = apiRequest;

  // ============================================
  // AUTHENTICATION
  // ============================================
  const updateGreeting = () => {
    const greet = document.getElementById("greet-text");
    const authTrig = document.getElementById("auth-trigger");
    if (!greet || !authTrig) return;

    const dropdownContainer = greet.closest(".dropdown") || authTrig.closest(".dropdown");
    if (!dropdownContainer) return;

    if (state.userId) {
      const name = state.fullName || state.email || "Người dùng";
      greet.textContent = `Xin chào, ${name}`;
      greet.style.display = "inline-block";
      authTrig.style.display = "none";

      let dropdownMenu = dropdownContainer.querySelector(".dropdown-menu");
      if (!dropdownMenu) {
        dropdownMenu = document.createElement("div");
        dropdownMenu.className = "dropdown-menu dropdown-menu-end";
        dropdownMenu.style.display = "none";
        dropdownMenu.innerHTML = `
          <a class="dropdown-item" href="profile.html">Thông tin người dùng</a>
          <a class="dropdown-item" href="orders.html">Lịch sử đơn hàng</a>
          <hr class="dropdown-divider">
          <a class="dropdown-item" href="#" id="logout-link">Đăng xuất</a>
        `;
        dropdownContainer.appendChild(dropdownMenu);
        document.getElementById("logout-link")?.addEventListener("click", (e) => {
          e.preventDefault();
          logout();
        });
      }

      if (window.getComputedStyle(dropdownContainer).position === "static") {
        dropdownContainer.style.position = "relative";
      }

      if (!dropdownContainer.dataset.hoverSetup) {
        dropdownContainer.dataset.hoverSetup = "true";
        dropdownContainer.addEventListener("mouseenter", () => {
          const menu = dropdownContainer.querySelector(".dropdown-menu");
          if (menu) menu.style.display = "block";
        });
        dropdownContainer.addEventListener("mouseleave", () => {
          const menu = dropdownContainer.querySelector(".dropdown-menu");
          if (menu) menu.style.display = "none";
        });
      }
    } else {
      greet.style.display = "none";
      authTrig.style.display = "";
      const dropdownMenu = dropdownContainer.querySelector(".dropdown-menu");
      if (dropdownMenu) dropdownMenu.remove();
      delete dropdownContainer.dataset.hoverSetup;
    }
  };

  const logout = () => {
    state.token = "";
    state.userId = null;
    state.role = "customer";
    state.fullName = "";
    state.email = "";
    localStorage.removeItem("token");
    localStorage.removeItem("userId");
    localStorage.removeItem("role");
    localStorage.removeItem("fullName");
    localStorage.removeItem("email");
    toggleAdminUI();
    updateGreeting();
    window.location.href = "index.html";
  };

  const toggleAdminUI = () => {
    const isAdmin = (state.role || "").toLowerCase() === "admin";
    document.querySelectorAll(".admin-only").forEach((el) => {
      el.style.display = isAdmin ? "" : "none";
    });
  };

  window.updateGreeting = updateGreeting;
  window.toggleAdminUI = toggleAdminUI;

  // ============================================
  // CART FUNCTIONS
  // ============================================
  const loadCartCount = async () => {
    if (!state.userId) {
      const cartCount = document.getElementById("cart-count");
      if (cartCount) cartCount.textContent = "0";
      return;
    }
    
    try {
      const cart = await apiRequest(`/api/cart?userId=${state.userId}`);
      const items = cart?.cartItems || cart?.CartItems || [];
      const count = items.reduce((sum, item) => sum + (item.quantity || item.Quantity || 0), 0);
      const cartCount = document.getElementById("cart-count");
      if (cartCount) cartCount.textContent = count;
    } catch (err) {
      console.error("Load cart count failed:", err);
      const cartCount = document.getElementById("cart-count");
      if (cartCount) cartCount.textContent = "0";
    }
  };

  // ============================================
  // RENDER FUNCTIONS
  // ============================================
  const renderBookCard = async (book) => {
    const bookId = book.bookId || book.BookId;
    const title = book.title || book.Title || "Chưa có tên";
    const originalPrice = book.price ?? book.Price ?? 0;
    const imageUrl = book.imageUrl || book.ImageUrl || "images/product-item1.png";
    const author = book.author || book.Author || {};
    const authorName = author.authorName || author.AuthorName || "";

    // Calculate discount
    const priceInfo = await calculateDiscountedPrice(bookId, originalPrice);
    const displayPrice = priceInfo.hasDiscount ? priceInfo.discountedPrice : originalPrice;
    const discountPercent = priceInfo.discountType === 'percentage' 
      ? priceInfo.discountValue 
      : priceInfo.discount > 0 
        ? Math.round((priceInfo.discount / originalPrice) * 100) 
        : 0;

    return `
      <div class="col-lg-3 col-md-4 col-sm-6 mb-4">
        <div class="product-card-luxury glass-card cursor-pointer" onclick="window.location.href='detail.html?id=${bookId}'">
          <div class="product-image-wrapper">
            ${priceInfo.hasDiscount ? `<span class="discount-badge">-${discountPercent}%</span>` : ''}
            <img src="${imageUrl}" class="product-image-luxury" alt="${title}" 
                 onerror="this.src='images/product-item1.png'">
          </div>
          <div class="product-info-luxury">
            <h6 class="product-title-luxury">${title}</h6>
            <p class="product-author-luxury">${authorName}</p>
            <div class="product-price-section">
              ${priceInfo.hasDiscount ? `
                <div class="price-old">${formatVND(originalPrice)}</div>
                <div class="price-new">${formatVND(displayPrice)}</div>
              ` : `
                <div class="price-normal">${formatVND(displayPrice)}</div>
              `}
            </div>
            <button class="btn-add-cart-luxury btn-add-cart" data-book-id="${bookId}" data-original-price="${originalPrice}" data-discounted-price="${displayPrice}" onclick="event.stopPropagation();">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
                <line x1="3" y1="6" x2="21" y2="6"></line>
                <path d="M16 10a4 4 0 0 1-8 0"></path>
              </svg>
              <span>Thêm vào giỏ</span>
            </button>
          </div>
        </div>
      </div>
    `;
  };

  const renderGrid = async (gridId, books = []) => {
    const grid = document.getElementById(gridId);
    if (!grid) return;

    if (!Array.isArray(books) || books.length === 0) {
      grid.innerHTML = `<div class="col-12"><p class="text-muted">Không có sách nào.</p></div>`;
      return;
    }

    // Render all cards asynchronously
    const cards = await Promise.all(books.map(book => renderBookCard(book)));
    grid.innerHTML = cards.join("");
    bindAddToCart();
  };

  window.renderGrid = renderGrid;

  const bindAddToCart = () => {
    document.querySelectorAll(".btn-add-cart").forEach(btn => {
      btn.onclick = async (e) => {
        e.preventDefault(); 
        if (!state.userId) {
          alert("Vui lòng đăng nhập trước khi thêm vào giỏ hàng.");
          window.location.href = "login.html";
          return;
        }
        const bookId = Number(btn.dataset.bookId);
        try {
          await apiRequest("/api/cart/add", {
            method: "POST",
            body: JSON.stringify({ userId: state.userId, bookId, quantity: 1 }),
          });
          alert("Đã thêm vào giỏ hàng");
          loadCartCount();
        } catch (err) {
          console.error("Add to cart failed:", err);
          alert("Không thể thêm vào giỏ hàng");
        }
      };
    });
  };

  // ============================================
  // INITIALIZATION
  // ============================================
  document.addEventListener("DOMContentLoaded", () => {
    updateGreeting();
    toggleAdminUI();
    loadCartCount();
  });

  window.loadCartCount = loadCartCount;

} // End of __booklyAppLoaded check

