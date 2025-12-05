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
  const renderBookCard = (book) => {
    const bookId = book.bookId || book.BookId;
    const title = book.title || book.Title || "Chưa có tên";
    const price = book.price ?? book.Price ?? 0;
    const imageUrl = book.imageUrl || book.ImageUrl || "images/product-item1.png";
    const author = book.author || book.Author || {};
    const authorName = author.authorName || author.AuthorName || "";

    return `
      <div class="col-lg-3 col-md-4 col-sm-6 mb-4">
        <div class="card h-100 p-3 border rounded-3 shadow-sm">
          <img src="${imageUrl}" class="img-fluid product-card-img" alt="${title}" 
               style="height: 220px; object-fit: cover; width: 100%;" 
               onerror="this.src='images/product-item1.png'">
          <h6 class="mt-3 mb-1 fw-bold">
            <a href="detail.html?id=${bookId}" class="text-decoration-none text-dark">${title}</a>
          </h6>
          <p class="text-black-50 mb-1">${authorName}</p>
          <div class="mb-2">
            <span class="price text-primary fw-bold">$${price.toFixed(2)}</span>
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-dark btn-sm btn-add-cart flex-grow-1" data-book-id="${bookId}">
              Thêm vào giỏ
            </button>
            <a href="detail.html?id=${bookId}" class="btn btn-outline-dark btn-sm">Chi tiết</a>
          </div>
        </div>
      </div>
    `;
  };

  const renderGrid = (gridId, books = []) => {
    const grid = document.getElementById(gridId);
    if (!grid) return;

    if (!Array.isArray(books) || books.length === 0) {
      grid.innerHTML = `<div class="col-12"><p class="text-muted">Không có sách nào.</p></div>`;
      return;
    }

    grid.innerHTML = books.map(book => renderBookCard(book)).join("");
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

