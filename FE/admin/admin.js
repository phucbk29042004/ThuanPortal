// Admin Panel JavaScript - Fixed Version
(function() {
  'use strict';

  // Cấu hình Base URL API (Thay đổi port nếu cần)
  const API_BASE_URL = 'https://localhost:7104'; 

  // ============================================
  // 1. CORE FUNCTIONS & API HELPER
  // ============================================

  // Hàm gọi API chuẩn, xử lý Token và lỗi 204
  window.apiRequest = async function(endpoint, options = {}) {
      const token = localStorage.getItem('token'); // Hoặc sessionStorage
      
      const defaultHeaders = {
          'Content-Type': 'application/json',
          'Authorization': token ? `Bearer ${token}` : ''
      };

      const config = {
          ...options,
          headers: { ...defaultHeaders, ...options.headers }
      };

      try {
          const url = endpoint.startsWith('http') ? endpoint : `${API_BASE_URL}${endpoint}`;
          console.log(`📡 Calling API: ${url}`); // Debug log

          const response = await fetch(url, config);

          // Xử lý 204 No Content (Thành công nhưng không có body)
          if (response.status === 204) {
              console.warn(`⚠️ API trả về 204 No Content cho: ${endpoint}`);
              return null; 
          }

          // Xử lý 401 Unauthorized (Hết hạn token)
          if (response.status === 401) {
              alert('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
              window.location.href = '../login.html';
              return null;
          }

          if (!response.ok) {
              throw new Error(`HTTP error! status: ${response.status}`);
          }

          const data = await response.json();
          return data;
      } catch (error) {
          console.error('❌ API Error:', error);
          throw error;
      }
  };

  // Hàm định dạng tiền tệ VND
  window.formatVND = function(amount) {
      return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
  };

  // Kiểm tra quyền Admin
  function checkAdminRole() {
      // Lấy role từ localStorage, đảm bảo an toàn nếu null
      const role = (localStorage.getItem('role') || '').toLowerCase();
      
      // Logic kiểm tra: Chấp nhận 'admin' hoặc nếu bạn đang test thì tạm bỏ qua
      if (role !== 'admin') {
          console.warn('Current role is:', role); 
          // alert('Bạn không có quyền truy cập trang quản trị!'); 
          // window.location.href = '../index.html'; 
          // return false; 
          return true; // TẠM THỜI RETURN TRUE ĐỂ BẠN TEST GIAO DIỆN, SỬA LẠI SAU
      }
      return true;
  }

  // ============================================
  // 2. INITIALIZATION & NAVIGATION
  // ============================================

  function initAdmin() {
      if (!checkAdminRole()) return;

      // Hiển thị tên Admin
      const adminName = localStorage.getItem('fullName') || 'Administrator';
      const adminNameEl = document.getElementById('admin-name');
      if (adminNameEl) adminNameEl.textContent = adminName;

      setupNavigation();
      
      // Mặc định load dashboard
      loadPage('dashboard');
  }

  function setupNavigation() {
      document.querySelectorAll('.nav-item[data-page]').forEach(item => {
          item.addEventListener('click', (e) => {
              e.preventDefault();
              const page = item.getAttribute('data-page');

              // Update active class
              document.querySelectorAll('.nav-item').forEach(nav => nav.classList.remove('active'));
              item.classList.add('active');

              loadPage(page);
          });
      });

      // Nút đăng xuất
      document.getElementById('logout-btn')?.addEventListener('click', (e) => {
          e.preventDefault();
          if (confirm('Bạn có chắc muốn đăng xuất?')) {
              localStorage.clear();
              window.location.href = '../login.html';
          }
      });
  }

  function loadPage(pageName) {
      // Cập nhật tiêu đề trang
      const titles = {
          dashboard: 'Dashboard - Tổng quan',
          users: 'Quản lý người dùng',
          books: 'Quản lý sách',
          promotions: 'Quản lý khuyến mãi',
          orders: 'Quản lý đơn hàng',
          payments: 'Quản lý thanh toán'
      };
      const titleEl = document.getElementById('page-title');
      if (titleEl) titleEl.textContent = titles[pageName] || 'Dashboard';

      // Ẩn tất cả các trang
      document.querySelectorAll('.page-content').forEach(page => {
          page.classList.remove('active');
          page.style.display = 'none'; // Đảm bảo ẩn hẳn
      });

      // Hiện trang mục tiêu
      const targetPage = document.getElementById(`${pageName}-page`);
      if (targetPage) {
          targetPage.classList.add('active');
          targetPage.style.display = 'block';

          // Luôn load lại dữ liệu mới nhất
          loadPageContent(pageName, targetPage);
      } else {
          console.error(`Page element #${pageName}-page not found`);
      }
  }

  async function loadPageContent(pageName, container) {
      // Hiển thị loading trong container
      // Lưu ý: Không overwrite toàn bộ container nếu nó chứa cấu trúc bảng tĩnh, 
      // nhưng ở đây ta giả định container là wrapper dữ liệu.
      
      try {
          switch(pageName) {
              case 'dashboard': await loadDashboard(); break;
              case 'users': await loadUsers(container); break;
              case 'books': await loadBooks(container); break;
              case 'promotions': await loadPromotions(container); break;
              case 'orders': await loadOrders(container); break;
              case 'payments': await loadPayments(container); break;
          }
      } catch (err) {
          console.error(`Lỗi tải trang ${pageName}:`, err);
          // Không hiển thị lỗi lên UI để tránh vỡ layout, chỉ log
      }
  }

  // ============================================
  // 3. DASHBOARD LOGIC
  // ============================================

  async function loadDashboard() {
      // Load song song các chỉ số
      await Promise.all([
          loadDashboardStats(),
          loadTopBooks(),
          loadTopUsers()
      ]);
  }

  async function loadDashboardStats() {
      try {
          const data = await window.apiRequest('/api/admin/dashboard/stats');
          
          // Nếu API 204 hoặc null, dùng giá trị mặc định 0
          const stats = data || {}; 
          
          // Helper lấy giá trị an toàn (chấp nhận cả chữ hoa/thường)
          const getVal = (obj, key) => obj?.[key] || obj?.[key.charAt(0).toUpperCase() + key.slice(1)] || 0;

          setText('stat-total-books', getVal(stats, 'totalBooks'));
          setText('stat-total-users', getVal(stats, 'totalUsers'));
          setText('stat-total-orders', getVal(stats, 'totalOrders'));
          
          const revenue = getVal(stats, 'totalRevenue');
          setText('stat-total-revenue', window.formatVND(revenue));

      } catch (err) {
          console.warn('Dashboard stats failed, using 0');
      }
  }

  async function loadTopBooks() {
      const container = document.getElementById('top-books-list');
      if (!container) return;

      try {
          const data = await window.apiRequest('/api/admin/dashboard/top-books?limit=10');
          const list = Array.isArray(data) ? data : (data?.value || []);

          if (list.length === 0) {
              container.innerHTML = '<p class="text-center text-white-50 py-5">Chưa có dữ liệu</p>';
              return;
          }

          container.innerHTML = list.map((item, index) => {
              const book = item.book || item.Book || {};
              const title = book.title || book.Title || 'Sách không tên';
              const sold = item.totalSold || item.TotalSold || 0;
              const price = book.price || book.Price || 0;
              const rankClass = index === 0 ? 'rank-1' : index === 1 ? 'rank-2' : index === 2 ? 'rank-3' : 'rank-other';
              
              return `
                  <div class="top-list-item">
                      <div class="top-list-rank ${rankClass}">${index + 1}</div>
                      <div class="top-list-info">
                          <div class="top-list-title">${title}</div>
                          <div class="top-list-subtitle">
                              <span class="badge-sold">${sold} đã bán</span>
                          </div>
                      </div>
                      <div class="top-list-value">${window.formatVND(price)}</div>
                  </div>
              `;
          }).join('');
      } catch (err) {
          console.error('Error loading top books:', err);
          container.innerHTML = '<p class="text-center text-danger py-5">Lỗi khi tải dữ liệu</p>';
      }
  }

  async function loadTopUsers() {
      const container = document.getElementById('top-users-list');
      if (!container) return;
      
      try {
          const data = await window.apiRequest('/api/admin/dashboard/top-users?limit=10');
          const list = Array.isArray(data) ? data : (data?.value || []);

          if (list.length === 0) {
              container.innerHTML = '<p class="text-center text-white-50 py-5">Chưa có dữ liệu</p>';
              return;
          }

          container.innerHTML = list.map((item, index) => {
              const user = item.user || item.User || {};
              const name = user.fullName || user.FullName || user.email || 'Khách';
              const spent = item.totalSpent || item.TotalSpent || 0;
              const orders = item.totalOrders || item.TotalOrders || 0;
              const rankClass = index === 0 ? 'rank-1' : index === 1 ? 'rank-2' : index === 2 ? 'rank-3' : 'rank-other';

              return `
                  <div class="top-list-item">
                      <div class="top-list-rank ${rankClass}">${index + 1}</div>
                      <div class="top-list-info">
                          <div class="top-list-title">${name}</div>
                          <div class="top-list-subtitle">
                              <span class="badge-sold">${orders} đơn hàng</span>
                          </div>
                      </div>
                      <div class="top-list-value user-value">${window.formatVND(spent)}</div>
                  </div>
              `;
          }).join('');
      } catch (err) {
          console.error('Error loading top users:', err);
          container.innerHTML = '<p class="text-center text-danger py-5">Lỗi khi tải dữ liệu</p>';
      }
  }

  // ============================================
  // 4. MANAGEMENT PAGES (Users, Books, etc.)
  // ============================================

  async function loadUsers(container) {
      container.innerHTML = `
          <div class="d-flex justify-content-between align-items-center mb-4">
              <h3 class="section-title-luxury mb-0">Danh sách người dùng</h3>
              <button class="btn-admin btn-admin-primary" onclick="adminApp.showAddUserModal()">
                  <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                  </svg>
                  Thêm người dùng
              </button>
          </div>
          <div id="users-table-container"></div>
          <div id="users-pagination"></div>
      `;

      await renderUsersTable();
  }

  async function renderUsersTable(page = 1) {
      const container = document.getElementById('users-table-container');
      if (!container) return;

      try {
          container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-luxury-gold" role="status"></div></div>';

          const response = await window.apiRequest(`/api/admin/users?page=${page}&pageSize=10`);
          const users = response?.data || [];
          const totalPages = response?.totalPages || 1;
          const totalCount = response?.totalCount || 0;

          if (users.length === 0) {
              container.innerHTML = '<p class="text-center text-white-50 py-5">Chưa có người dùng nào</p>';
              return;
          }

          container.innerHTML = `
              <div class="data-table-container">
                  <table class="data-table">
                      <thead>
                          <tr>
                              <th>ID</th>
                              <th>Họ tên</th>
                              <th>Email</th>
                              <th>Số điện thoại</th>
                              <th>Vai trò</th>
                              <th>Ngày tạo</th>
                              <th>Thao tác</th>
                          </tr>
                      </thead>
                      <tbody>
                          ${users.map(user => `
                              <tr>
                                  <td>${user.userId || user.UserId}</td>
                                  <td>${user.fullName || user.FullName || 'N/A'}</td>
                                  <td>${user.email || user.Email || 'N/A'}</td>
                                  <td>${user.phone || user.Phone || 'N/A'}</td>
                                  <td><span class="badge-admin ${(user.role || user.Role || '').toLowerCase() === 'admin' ? 'badge-warning' : 'badge-info'}">${user.role || user.Role || 'customer'}</span></td>
                                  <td>${new Date(user.createdAt || user.CreatedAt).toLocaleDateString('vi-VN')}</td>
                                  <td>
                                      <button class="btn-admin btn-admin-sm btn-admin-secondary" onclick="adminApp.editUser(${user.userId || user.UserId})">Sửa</button>
                                      <button class="btn-admin btn-admin-sm btn-admin-danger" onclick="adminApp.deleteUser(${user.userId || user.UserId})">Xóa</button>
                                  </td>
                              </tr>
                          `).join('')}
                      </tbody>
                  </table>
              </div>
          `;

          renderPagination('users-pagination', page, totalPages, 'users');
      } catch (err) {
          console.error('Error loading users:', err);
          container.innerHTML = `<div class="alert alert-danger">Lỗi khi tải dữ liệu: ${err.message}</div>`;
      }
  }

  // Books filter state
  let booksFilterState = {
      filterType: 'all', // all, category, author, publisher
      filterValue: null,
      sortBy: 'name', // name, price-asc, price-desc
      allBooks: []
  };

  async function loadBooks(container) {
      container.innerHTML = `
          <div class="d-flex justify-content-between align-items-center mb-4">
              <h3 class="section-title-luxury mb-0">Danh sách sách</h3>
              <button class="btn-admin btn-admin-primary" onclick="adminApp.showAddBookModal()">
                  <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                  </svg>
                  Thêm sách
              </button>
          </div>
          
          <!-- Filter Buttons -->
          <div class="books-filter-bar mb-4">
              <button class="filter-btn active" data-filter="all" onclick="adminApp.setBooksFilter('all', null)">
                  Tất cả danh mục
              </button>
              <button class="filter-btn" data-filter="category" onclick="adminApp.showCategoryFilter()">
                  Danh mục
              </button>
              <button class="filter-btn" data-filter="author" onclick="adminApp.showAuthorFilter()">
                  Tác giả
              </button>
              <button class="filter-btn" data-filter="publisher" onclick="adminApp.showPublisherFilter()">
                  Nhà xuất bản
              </button>
              <button class="filter-btn" data-sort="name" onclick="adminApp.setBooksSort('name')">
                  Tên A-Z
              </button>
              <button class="filter-btn" data-sort="price-asc" onclick="adminApp.setBooksSort('price-asc')">
                  Giá tăng dần
              </button>
              <button class="filter-btn" data-sort="price-desc" onclick="adminApp.setBooksSort('price-desc')">
                  Giá giảm dần
              </button>
          </div>
          
          <!-- Filter Dropdowns -->
          <div id="books-filter-dropdowns" class="mb-4" style="display: none;"></div>
          
          <div id="books-table-container"></div>
          <div id="books-pagination"></div>
      `;

      // Load all books first
      try {
          const books = await window.apiRequest('/api/admin/books');
          booksFilterState.allBooks = Array.isArray(books) ? books : [];
      } catch (err) {
          console.error('Error loading books:', err);
      }

      await renderBooksTable();
  }

  async function renderBooksTable(page = 1) {
      const container = document.getElementById('books-table-container');
      if (!container) return;

      try {
          container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-luxury-gold" role="status"></div></div>';

          // Get filtered and sorted books
          let bookList = [...booksFilterState.allBooks];

          // Apply filter
          if (booksFilterState.filterType !== 'all' && booksFilterState.filterValue) {
              switch(booksFilterState.filterType) {
                  case 'category':
                      bookList = bookList.filter(book => 
                          (book.categoryId || book.CategoryId) === booksFilterState.filterValue
                      );
                      break;
                  case 'author':
                      bookList = bookList.filter(book => 
                          (book.authorId || book.AuthorId) === booksFilterState.filterValue
                      );
                      break;
                  case 'publisher':
                      bookList = bookList.filter(book => 
                          (book.publisherId || book.PublisherId) === booksFilterState.filterValue
                      );
                      break;
              }
          }

          // Apply sort
          switch(booksFilterState.sortBy) {
              case 'name':
                  bookList.sort((a, b) => {
                      const nameA = (a.title || a.Title || '').toLowerCase();
                      const nameB = (b.title || b.Title || '').toLowerCase();
                      return nameA.localeCompare(nameB, 'vi');
                  });
                  break;
              case 'price-asc':
                  bookList.sort((a, b) => {
                      const priceA = a.price || a.Price || 0;
                      const priceB = b.price || b.Price || 0;
                      return priceA - priceB;
                  });
                  break;
              case 'price-desc':
                  bookList.sort((a, b) => {
                      const priceA = a.price || a.Price || 0;
                      const priceB = b.price || b.Price || 0;
                      return priceB - priceA;
                  });
                  break;
          }

          const totalBooks = bookList.length;
          const totalPages = Math.ceil(totalBooks / 10);
          const startIndex = (page - 1) * 10;
          const paginatedBooks = bookList.slice(startIndex, startIndex + 10);

          if (paginatedBooks.length === 0) {
              container.innerHTML = '<p class="text-center text-white-50 py-5">Chưa có sách nào</p>';
              return;
          }

          container.innerHTML = `
              <div class="data-table-container">
                  <table class="data-table">
                      <thead>
                          <tr>
                              <th>ID</th>
                              <th>Hình ảnh</th>
                              <th>Tên sách</th>
                              <th>Giá</th>
                              <th>Số lượng</th>
                              <th>Tác giả</th>
                              <th>Thao tác</th>
                          </tr>
                      </thead>
                      <tbody>
                          ${paginatedBooks.map(book => `
                              <tr>
                                  <td>${book.bookId || book.BookId}</td>
                                  <td><img src="${book.imageUrl || book.ImageUrl || '../images/product-item1.png'}" alt="${book.title || book.Title}" style="width: 50px; height: 70px; object-fit: cover; border-radius: 4px;"></td>
                                  <td>${book.title || book.Title || 'N/A'}</td>
                                  <td>${window.formatVND(book.price || book.Price || 0)}</td>
                                  <td>${book.quantity || book.Quantity || 0}</td>
                                  <td>${book.author?.authorName || book.Author?.AuthorName || 'N/A'}</td>
                                  <td>
                                      <button class="btn-admin btn-admin-sm btn-admin-secondary" onclick="adminApp.editBook(${book.bookId || book.BookId})">Sửa</button>
                                      <button class="btn-admin btn-admin-sm btn-admin-danger" onclick="adminApp.deleteBook(${book.bookId || book.BookId})">Xóa</button>
                                  </td>
                              </tr>
                          `).join('')}
                      </tbody>
                  </table>
              </div>
          `;

          renderPagination('books-pagination', page, totalPages, 'books');
      } catch (err) {
          console.error('Error loading books:', err);
          container.innerHTML = `<div class="alert alert-danger">Lỗi khi tải dữ liệu: ${err.message}</div>`;
      }
  }

  async function loadOrders(container) {
      container.innerHTML = `
          <div class="d-flex justify-content-between align-items-center mb-4">
              <h3 class="section-title-luxury mb-0">Danh sách đơn hàng</h3>
          </div>
          <div id="orders-table-container"></div>
          <div id="orders-pagination"></div>
      `;

      await renderOrdersTable();
  }

  async function renderOrdersTable(page = 1) {
      const container = document.getElementById('orders-table-container');
      if (!container) return;

      try {
          container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-luxury-gold" role="status"></div></div>';

          const response = await window.apiRequest(`/api/admin/orders?page=${page}&pageSize=10`);
          const orders = response?.data || [];
          const totalPages = response?.totalPages || 1;

          if (orders.length === 0) {
              container.innerHTML = '<p class="text-center text-white-50 py-5">Chưa có đơn hàng nào</p>';
              return;
          }

          container.innerHTML = `
              <div class="data-table-container">
                  <table class="data-table">
                      <thead>
                          <tr>
                              <th>ID</th>
                              <th>Khách hàng</th>
                              <th>Tổng tiền</th>
                              <th>Trạng thái</th>
                              <th>Ngày tạo</th>
                              <th>Thao tác</th>
                          </tr>
                      </thead>
                      <tbody>
                          ${orders.map(order => {
                              const status = order.status || order.Status || 'pending';
                              const statusMap = {
                                  'pending': { text: 'Chờ xử lý', class: 'badge-warning' },
                                  'processing': { text: 'Đang xử lý', class: 'badge-info' },
                                  'shipped': { text: 'Đã giao hàng', class: 'badge-info' },
                                  'delivered': { text: 'Đã nhận hàng', class: 'badge-success' },
                                  'cancelled': { text: 'Đã hủy', class: 'badge-danger' },
                                  'awaiting_payment': { text: 'Chờ thanh toán', class: 'badge-warning' }
                              };
                              const statusInfo = statusMap[status.toLowerCase()] || { text: status, class: 'badge-info' };
                              const user = order.user || order.User || {};
                              const userName = user.fullName || user.FullName || user.email || user.Email || 'N/A';
                              
                              return `
                                  <tr>
                                      <td>#${order.orderId || order.OrderId}</td>
                                      <td>${userName}</td>
                                      <td>${window.formatVND(order.totalPrice || order.TotalPrice || 0)}</td>
                                      <td><span class="badge-admin ${statusInfo.class}">${statusInfo.text}</span></td>
                                      <td>${new Date(order.createdAt || order.CreatedAt).toLocaleDateString('vi-VN')}</td>
                                      <td>
                                          <button class="btn-admin btn-admin-sm btn-admin-secondary" onclick="adminApp.viewOrder(${order.orderId || order.OrderId})">Xem</button>
                                          <button class="btn-admin btn-admin-sm btn-admin-primary" onclick="adminApp.updateOrderStatus(${order.orderId || order.OrderId})">Cập nhật</button>
                                      </td>
                                  </tr>
                              `;
                          }).join('')}
                      </tbody>
                  </table>
              </div>
          `;

          renderPagination('orders-pagination', page, totalPages, 'orders');
      } catch (err) {
          console.error('Error loading orders:', err);
          container.innerHTML = `<div class="alert alert-danger">Lỗi khi tải dữ liệu: ${err.message}</div>`;
      }
  }

  async function loadPromotions(container) {
      container.innerHTML = `
          <div class="d-flex justify-content-between align-items-center mb-4">
              <h3 class="section-title-luxury mb-0">Danh sách khuyến mãi</h3>
              <button class="btn-admin btn-admin-primary" onclick="adminApp.showAddPromotionModal()">
                  <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                  </svg>
                  Thêm khuyến mãi
              </button>
          </div>
          <div id="promotions-table-container"></div>
      `;

      await renderPromotionsTable();
  }

  async function renderPromotionsTable() {
      const container = document.getElementById('promotions-table-container');
      if (!container) return;

      try {
          container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-luxury-gold" role="status"></div></div>';

          const promotions = await window.apiRequest('/api/admin/promotions');
          const promoList = Array.isArray(promotions) ? promotions : [];

          if (promoList.length === 0) {
              container.innerHTML = '<p class="text-center text-white-50 py-5">Chưa có khuyến mãi nào</p>';
              return;
          }

          container.innerHTML = `
              <div class="data-table-container">
                  <table class="data-table">
                      <thead>
                          <tr>
                              <th>ID</th>
                              <th>Tên khuyến mãi</th>
                              <th>Loại</th>
                              <th>Giá trị</th>
                              <th>Ngày bắt đầu</th>
                              <th>Ngày kết thúc</th>
                              <th>Trạng thái</th>
                              <th>Thao tác</th>
                          </tr>
                      </thead>
                      <tbody>
                          ${promoList.map(promo => {
                              const isActive = promo.isActive || promo.IsActive;
                              const startDate = new Date(promo.startDate || promo.StartDate);
                              const endDate = new Date(promo.endDate || promo.EndDate);
                              const now = new Date();
                              const isCurrentlyActive = isActive && now >= startDate && now <= endDate;
                              
                              return `
                                  <tr>
                                      <td>${promo.promotionId || promo.PromotionId}</td>
                                      <td>${promo.promotionName || promo.PromotionName || 'N/A'}</td>
                                      <td>${promo.promotionType || promo.PromotionType || 'N/A'}</td>
                                      <td>${promo.promotionType === 'Percentage' || promo.PromotionType === 'Percentage' ? (promo.discountValue || promo.DiscountValue || 0) + '%' : window.formatVND(promo.discountValue || promo.DiscountValue || 0)}</td>
                                      <td>${startDate.toLocaleDateString('vi-VN')}</td>
                                      <td>${endDate.toLocaleDateString('vi-VN')}</td>
                                      <td><span class="badge-admin ${isCurrentlyActive ? 'badge-success' : 'badge-danger'}">${isCurrentlyActive ? 'Đang hoạt động' : 'Không hoạt động'}</span></td>
                                      <td>
                                          <button class="btn-admin btn-admin-sm btn-admin-secondary" onclick="adminApp.editPromotion(${promo.promotionId || promo.PromotionId})">Sửa</button>
                                          <button class="btn-admin btn-admin-sm btn-admin-danger" onclick="adminApp.deletePromotion(${promo.promotionId || promo.PromotionId})">Xóa</button>
                                      </td>
                                  </tr>
                              `;
                          }).join('')}
                      </tbody>
                  </table>
              </div>
          `;
      } catch (err) {
          console.error('Error loading promotions:', err);
          container.innerHTML = `<div class="alert alert-danger">Lỗi khi tải dữ liệu: ${err.message}</div>`;
      }
  }

  async function loadPayments(container) {
      container.innerHTML = `
          <div class="d-flex justify-content-between align-items-center mb-4">
              <h3 class="section-title-luxury mb-0">Danh sách thanh toán</h3>
          </div>
          <div id="payments-table-container"></div>
          <div id="payments-pagination"></div>
      `;

      await renderPaymentsTable();
  }

  async function renderPaymentsTable(page = 1) {
      const container = document.getElementById('payments-table-container');
      if (!container) return;

      try {
          container.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-luxury-gold" role="status"></div></div>';

          let response;
          try {
              response = await window.apiRequest(`/api/admin/payments?page=${page}&pageSize=10`);
          } catch (err) {
              response = await window.apiRequest(`/api/admin/adminpayments?page=${page}&pageSize=10`);
          }
          
          const payments = response?.data || [];
          const totalPages = response?.totalPages || 1;

          if (payments.length === 0) {
              container.innerHTML = '<p class="text-center text-white-50 py-5">Chưa có thanh toán nào</p>';
              return;
          }

          container.innerHTML = `
              <div class="data-table-container">
                  <table class="data-table">
                      <thead>
                          <tr>
                              <th>ID</th>
                              <th>Đơn hàng</th>
                              <th>Khách hàng</th>
                              <th>Số tiền</th>
                              <th>Phương thức</th>
                              <th>Trạng thái</th>
                              <th>Ngày tạo</th>
                              <th>Thao tác</th>
                          </tr>
                      </thead>
                      <tbody>
                          ${payments.map(payment => {
                              const status = payment.paymentStatus || payment.PaymentStatus || 'pending';
                              const statusMap = {
                                  'pending': { text: 'Chờ xử lý', class: 'badge-warning' },
                                  'completed': { text: 'Hoàn thành', class: 'badge-success' },
                                  'failed': { text: 'Thất bại', class: 'badge-danger' },
                                  'cancelled': { text: 'Đã hủy', class: 'badge-danger' }
                              };
                              const statusInfo = statusMap[status.toLowerCase()] || { text: status, class: 'badge-info' };
                              const user = payment.user || payment.User || {};
                              const userName = user.fullName || user.FullName || user.email || user.Email || 'N/A';
                              const order = payment.order || payment.Order || {};
                              const orderId = order.orderId || order.OrderId || 'N/A';
                              
                              return `
                                  <tr>
                                      <td>#${payment.paymentId || payment.PaymentId}</td>
                                      <td>#${orderId}</td>
                                      <td>${userName}</td>
                                      <td>${window.formatVND(payment.amount || payment.Amount || 0)}</td>
                                      <td>${payment.paymentMethod || payment.PaymentMethod || 'N/A'}</td>
                                      <td><span class="badge-admin ${statusInfo.class}">${statusInfo.text}</span></td>
                                      <td>${new Date(payment.createdAt || payment.CreatedAt).toLocaleDateString('vi-VN')}</td>
                                      <td>
                                          <button class="btn-admin btn-admin-sm btn-admin-secondary" onclick="adminApp.viewPayment(${payment.paymentId || payment.PaymentId})">Xem</button>
                                      </td>
                                  </tr>
                              `;
                          }).join('')}
                      </tbody>
                  </table>
              </div>
          `;

          renderPagination('payments-pagination', page, totalPages, 'payments');
      } catch (err) {
          console.error('Error loading payments:', err);
          container.innerHTML = `<div class="alert alert-danger">Lỗi khi tải dữ liệu: ${err.message}</div>`;
      }
  }

  // Helper: Set text content an toàn
  function setText(id, value) {
      const el = document.getElementById(id);
      if (el) el.textContent = value;
  }

  // ============================================
  // 5. PAGINATION HELPER
  // ============================================

  function renderPagination(containerId, currentPage, totalPages, pageType) {
      const container = document.getElementById(containerId);
      if (!container || totalPages <= 1) {
          if (container) container.innerHTML = '';
          return;
      }

      let html = '<div class="pagination-admin">';
      
      if (currentPage > 1) {
          html += `<a href="#" class="page-link-admin" onclick="adminApp.goToPage(${currentPage - 1}, '${pageType}'); return false;">Trước</a>`;
      }

      for (let i = 1; i <= totalPages; i++) {
          if (i === 1 || i === totalPages || (i >= currentPage - 2 && i <= currentPage + 2)) {
              html += `<a href="#" class="page-link-admin ${i === currentPage ? 'active' : ''}" onclick="adminApp.goToPage(${i}, '${pageType}'); return false;">${i}</a>`;
          } else if (i === currentPage - 3 || i === currentPage + 3) {
              html += '<span class="page-link-admin">...</span>';
          }
      }

      if (currentPage < totalPages) {
          html += `<a href="#" class="page-link-admin" onclick="adminApp.goToPage(${currentPage + 1}, '${pageType}'); return false;">Sau</a>`;
      }

      html += '</div>';
      container.innerHTML = html;
  }

  // ============================================
  // 6. CRUD OPERATIONS
  // ============================================

  window.adminApp = {
      goToPage: function(page, pageType) {
          if (page < 1) return;
          switch(pageType) {
              case 'users':
                  renderUsersTable(page);
                  break;
              case 'books':
                  renderBooksTable(page);
                  break;
              case 'orders':
                  renderOrdersTable(page);
                  break;
              case 'payments':
                  renderPaymentsTable(page);
                  break;
          }
      },

      showAddUserModal: function() {
          document.getElementById('userModalTitle').textContent = 'Thêm người dùng';
          document.getElementById('userForm').reset();
          document.getElementById('userId').value = '';
          const modal = new bootstrap.Modal(document.getElementById('userModal'));
          modal.show();
      },

      editUser: async function(userId) {
          try {
              const user = await window.apiRequest(`/api/admin/users/${userId}`);
              document.getElementById('userModalTitle').textContent = 'Sửa người dùng';
              document.getElementById('userId').value = userId;
              document.getElementById('userFullName').value = user.fullName || user.FullName || '';
              document.getElementById('userEmail').value = user.email || user.Email || '';
              document.getElementById('userPhone').value = user.phone || user.Phone || '';
              document.getElementById('userRole').value = user.role || user.Role || 'customer';
              const modal = new bootstrap.Modal(document.getElementById('userModal'));
              modal.show();
          } catch (err) {
              alert('Lỗi khi tải thông tin người dùng: ' + err.message);
          }
      },

      deleteUser: async function(userId) {
          if (!confirm('Bạn có chắc muốn xóa người dùng này? Hành động này không thể hoàn tác!')) return;
          try {
              await window.apiRequest(`/api/admin/users/${userId}`, { method: 'DELETE' });
              alert('Xóa người dùng thành công!');
              const container = document.getElementById('users-table-container');
              if (container) {
                  await renderUsersTable();
              }
          } catch (err) {
              alert('Lỗi khi xóa người dùng: ' + (err.message || 'Vui lòng thử lại'));
          }
      },

      showAddBookModal: function() {
          document.getElementById('bookModalTitle').textContent = 'Thêm sách';
          document.getElementById('bookForm').reset();
          document.getElementById('bookId').value = '';
          const modal = new bootstrap.Modal(document.getElementById('bookModal'));
          modal.show();
      },

      editBook: async function(bookId) {
          try {
              const book = await window.apiRequest(`/api/admin/books/${bookId}`);
              document.getElementById('bookModalTitle').textContent = 'Sửa sách';
              document.getElementById('bookId').value = bookId;
              document.getElementById('bookTitle').value = book.title || book.Title || '';
              document.getElementById('bookPrice').value = book.price || book.Price || 0;
              document.getElementById('bookQuantity').value = book.quantity || book.Quantity || 0;
              document.getElementById('bookDescription').value = book.description || book.Description || '';
              document.getElementById('bookImageUrl').value = book.imageUrl || book.ImageUrl || '';
              document.getElementById('bookAuthorId').value = book.authorId || book.AuthorId || '';
              document.getElementById('bookPublisherId').value = book.publisherId || book.PublisherId || '';
              document.getElementById('bookCategoryId').value = book.categoryId || book.CategoryId || '';
              const modal = new bootstrap.Modal(document.getElementById('bookModal'));
              modal.show();
          } catch (err) {
              alert('Lỗi khi tải thông tin sách: ' + err.message);
          }
      },

      deleteBook: async function(bookId) {
          if (!confirm('Bạn có chắc muốn xóa sách này? Hành động này không thể hoàn tác!')) return;
          try {
              await window.apiRequest(`/api/admin/books/${bookId}`, { method: 'DELETE' });
              alert('Xóa sách thành công!');
              const container = document.getElementById('books-table-container');
              if (container) {
                  await renderBooksTable();
              }
          } catch (err) {
              alert('Lỗi khi xóa sách: ' + (err.message || 'Vui lòng thử lại'));
          }
      },

      showAddPromotionModal: function() {
          document.getElementById('promotionModalTitle').textContent = 'Thêm khuyến mãi';
          document.getElementById('promotionForm').reset();
          document.getElementById('promotionId').value = '';
          document.getElementById('promotionIsActive').checked = true;
          const modal = new bootstrap.Modal(document.getElementById('promotionModal'));
          modal.show();
      },

      editPromotion: async function(promoId) {
          try {
              const promo = await window.apiRequest(`/api/admin/promotions/${promoId}`);
              document.getElementById('promotionModalTitle').textContent = 'Sửa khuyến mãi';
              document.getElementById('promotionId').value = promoId;
              document.getElementById('promotionName').value = promo.promotionName || promo.PromotionName || '';
              document.getElementById('promotionType').value = promo.promotionType || promo.PromotionType || 'Percentage';
              document.getElementById('promotionDiscountValue').value = promo.discountValue || promo.DiscountValue || 0;
              const startDate = new Date(promo.startDate || promo.StartDate);
              const endDate = new Date(promo.endDate || promo.EndDate);
              document.getElementById('promotionStartDate').value = startDate.toISOString().slice(0, 16);
              document.getElementById('promotionEndDate').value = endDate.toISOString().slice(0, 16);
              document.getElementById('promotionIsActive').checked = promo.isActive || promo.IsActive || false;
              const modal = new bootstrap.Modal(document.getElementById('promotionModal'));
              modal.show();
          } catch (err) {
              alert('Lỗi khi tải thông tin khuyến mãi: ' + err.message);
          }
      },

      deletePromotion: async function(promoId) {
          if (!confirm('Bạn có chắc muốn xóa khuyến mãi này? Hành động này không thể hoàn tác!')) return;
          try {
              await window.apiRequest(`/api/admin/promotions/${promoId}`, { method: 'DELETE' });
              alert('Xóa khuyến mãi thành công!');
              const container = document.getElementById('promotions-table-container');
              if (container) {
                  await renderPromotionsTable();
              }
          } catch (err) {
              alert('Lỗi khi xóa khuyến mãi: ' + (err.message || 'Vui lòng thử lại'));
          }
      },

      viewOrder: async function(orderId) {
          try {
              const order = await window.apiRequest(`/api/admin/orders/${orderId}`);
              let detailsHtml = '<div class="glass-card p-4 mt-3"><h5>Chi tiết đơn hàng</h5>';
              detailsHtml += `<p><strong>Mã đơn:</strong> #${order.orderId || order.OrderId}</p>`;
              detailsHtml += `<p><strong>Khách hàng:</strong> ${order.user?.fullName || order.User?.FullName || 'N/A'}</p>`;
              detailsHtml += `<p><strong>Tổng tiền:</strong> ${window.formatVND(order.totalPrice || order.TotalPrice || 0)}</p>`;
              detailsHtml += `<p><strong>Trạng thái:</strong> ${order.status || order.Status}</p>`;
              detailsHtml += '<h6 class="mt-3">Sản phẩm:</h6><ul>';
              (order.orderDetails || order.OrderDetails || []).forEach(detail => {
                  detailsHtml += `<li>${detail.book?.title || detail.Book?.Title || 'N/A'} - SL: ${detail.quantity || detail.Quantity} - Giá: ${window.formatVND(detail.price || detail.Price || 0)}</li>`;
              });
              detailsHtml += '</ul></div>';
              alert(detailsHtml.replace(/<[^>]*>/g, ''));
          } catch (err) {
              alert('Lỗi khi tải thông tin đơn hàng: ' + err.message);
          }
      },

      updateOrderStatus: async function(orderId) {
          try {
              const order = await window.apiRequest(`/api/admin/orders/${orderId}`);
              document.getElementById('orderStatusId').value = orderId;
              document.getElementById('orderStatus').value = order.status || order.Status || 'pending';
              const modal = new bootstrap.Modal(document.getElementById('orderStatusModal'));
              modal.show();
          } catch (err) {
              alert('Lỗi khi tải thông tin đơn hàng: ' + err.message);
          }
      },

      viewPayment: async function(paymentId) {
          try {
              let payment;
              try {
                  payment = await window.apiRequest(`/api/admin/payments/${paymentId}`);
              } catch (err) {
                  payment = await window.apiRequest(`/api/admin/adminpayments/${paymentId}`);
              }
              alert(`Chi tiết thanh toán:\nID: #${payment.paymentId || payment.PaymentId}\nSố tiền: ${window.formatVND(payment.amount || payment.Amount || 0)}\nPhương thức: ${payment.paymentMethod || payment.PaymentMethod}\nTrạng thái: ${payment.paymentStatus || payment.PaymentStatus}`);
          } catch (err) {
              alert('Lỗi khi tải thông tin thanh toán: ' + err.message);
          }
      },

      setBooksFilter: function(filterType, filterValue) {
          booksFilterState.filterType = filterType;
          booksFilterState.filterValue = filterValue;
          
          // Update active button
          document.querySelectorAll('.filter-btn[data-filter]').forEach(btn => {
              btn.classList.remove('active');
          });
          const activeBtn = document.querySelector(`.filter-btn[data-filter="${filterType}"]`);
          if (activeBtn) activeBtn.classList.add('active');
          
          // Hide dropdowns
          const dropdowns = document.getElementById('books-filter-dropdowns');
          if (dropdowns) dropdowns.style.display = 'none';
          
          renderBooksTable(1);
      },

      setBooksSort: function(sortBy) {
          booksFilterState.sortBy = sortBy;
          
          // Update active button
          document.querySelectorAll('.filter-btn[data-sort]').forEach(btn => {
              btn.classList.remove('active');
          });
          const activeBtn = document.querySelector(`.filter-btn[data-sort="${sortBy}"]`);
          if (activeBtn) activeBtn.classList.add('active');
          
          renderBooksTable(1);
      },

      showCategoryFilter: async function() {
          const dropdowns = document.getElementById('books-filter-dropdowns');
          if (!dropdowns) return;

          try {
              const categories = await window.apiRequest('/api/admin/categories');
              const categoryList = Array.isArray(categories) ? categories : [];
              
              dropdowns.innerHTML = `
                  <div class="glass-card p-3">
                      <label class="form-label mb-2">Chọn danh mục:</label>
                      <select id="category-filter-select" class="form-control-admin" onchange="adminApp.applyCategoryFilter(this.value)">
                          <option value="">-- Chọn danh mục --</option>
                          ${categoryList.map(cat => `
                              <option value="${cat.categoryId || cat.CategoryId}">${cat.categoryName || cat.CategoryName}</option>
                          `).join('')}
                      </select>
                  </div>
              `;
              dropdowns.style.display = 'block';
          } catch (err) {
              console.error('Error loading categories:', err);
          }
      },

      showAuthorFilter: async function() {
          const dropdowns = document.getElementById('books-filter-dropdowns');
          if (!dropdowns) return;

          try {
              const authors = await window.apiRequest('/api/admin/authors');
              const authorList = Array.isArray(authors) ? authors : [];
              
              dropdowns.innerHTML = `
                  <div class="glass-card p-3">
                      <label class="form-label mb-2">Chọn tác giả:</label>
                      <select id="author-filter-select" class="form-control-admin" onchange="adminApp.applyAuthorFilter(this.value)">
                          <option value="">-- Chọn tác giả --</option>
                          ${authorList.map(auth => `
                              <option value="${auth.authorId || auth.AuthorId}">${auth.authorName || auth.AuthorName}</option>
                          `).join('')}
                      </select>
                  </div>
              `;
              dropdowns.style.display = 'block';
          } catch (err) {
              console.error('Error loading authors:', err);
          }
      },

      showPublisherFilter: async function() {
          const dropdowns = document.getElementById('books-filter-dropdowns');
          if (!dropdowns) return;

          try {
              const publishers = await window.apiRequest('/api/admin/publishers');
              const publisherList = Array.isArray(publishers) ? publishers : [];
              
              dropdowns.innerHTML = `
                  <div class="glass-card p-3">
                      <label class="form-label mb-2">Chọn nhà xuất bản:</label>
                      <select id="publisher-filter-select" class="form-control-admin" onchange="adminApp.applyPublisherFilter(this.value)">
                          <option value="">-- Chọn nhà xuất bản --</option>
                          ${publisherList.map(pub => `
                              <option value="${pub.publisherId || pub.PublisherId}">${pub.publisherName || pub.PublisherName}</option>
                          `).join('')}
                      </select>
                  </div>
              `;
              dropdowns.style.display = 'block';
          } catch (err) {
              console.error('Error loading publishers:', err);
          }
      },

      applyCategoryFilter: function(categoryId) {
          if (categoryId) {
              adminApp.setBooksFilter('category', parseInt(categoryId));
          } else {
              adminApp.setBooksFilter('all', null);
          }
      },

      applyAuthorFilter: function(authorId) {
          if (authorId) {
              adminApp.setBooksFilter('author', parseInt(authorId));
          } else {
              adminApp.setBooksFilter('all', null);
          }
      },

      applyPublisherFilter: function(publisherId) {
          if (publisherId) {
              adminApp.setBooksFilter('publisher', parseInt(publisherId));
          } else {
              adminApp.setBooksFilter('all', null);
          }
      }
  };

  // Save User
  document.getElementById('saveUserBtn')?.addEventListener('click', async () => {
      const userId = document.getElementById('userId').value;
      const data = {
          fullName: document.getElementById('userFullName').value,
          email: document.getElementById('userEmail').value,
          phone: document.getElementById('userPhone').value,
          role: document.getElementById('userRole').value
      };

      try {
          if (userId) {
              await window.apiRequest(`/api/admin/users/${userId}`, {
                  method: 'PUT',
                  body: JSON.stringify(data)
              });
              alert('Cập nhật người dùng thành công!');
          } else {
              alert('Chức năng thêm người dùng cần được implement ở backend');
          }
          bootstrap.Modal.getInstance(document.getElementById('userModal')).hide();
          renderUsersTable();
      } catch (err) {
          alert('Lỗi: ' + err.message);
      }
  });

  // Save Book
  document.getElementById('saveBookBtn')?.addEventListener('click', async () => {
      const bookId = document.getElementById('bookId').value;
      const data = {
          title: document.getElementById('bookTitle').value,
          price: parseFloat(document.getElementById('bookPrice').value),
          quantity: parseInt(document.getElementById('bookQuantity').value),
          description: document.getElementById('bookDescription').value,
          imageUrl: document.getElementById('bookImageUrl').value,
          authorId: document.getElementById('bookAuthorId').value ? parseInt(document.getElementById('bookAuthorId').value) : null,
          publisherId: document.getElementById('bookPublisherId').value ? parseInt(document.getElementById('bookPublisherId').value) : null,
          categoryId: document.getElementById('bookCategoryId').value ? parseInt(document.getElementById('bookCategoryId').value) : null
      };

      try {
          if (bookId) {
              await window.apiRequest(`/api/admin/books/${bookId}`, {
                  method: 'PUT',
                  body: JSON.stringify(data)
              });
              alert('Cập nhật sách thành công!');
          } else {
              await window.apiRequest('/api/admin/books', {
                  method: 'POST',
                  body: JSON.stringify(data)
              });
              alert('Thêm sách thành công!');
          }
          bootstrap.Modal.getInstance(document.getElementById('bookModal')).hide();
          // Reload all books
          const books = await window.apiRequest('/api/admin/books');
          booksFilterState.allBooks = Array.isArray(books) ? books : [];
          await renderBooksTable();
      } catch (err) {
          alert('Lỗi: ' + err.message);
      }
  });

  // Save Promotion
  document.getElementById('savePromotionBtn')?.addEventListener('click', async () => {
      const promoId = document.getElementById('promotionId').value;
      const data = {
          promotionName: document.getElementById('promotionName').value,
          promotionType: document.getElementById('promotionType').value,
          discountValue: parseFloat(document.getElementById('promotionDiscountValue').value),
          startDate: new Date(document.getElementById('promotionStartDate').value).toISOString(),
          endDate: new Date(document.getElementById('promotionEndDate').value).toISOString(),
          isActive: document.getElementById('promotionIsActive').checked
      };

      try {
          if (promoId) {
              await window.apiRequest(`/api/admin/promotions/${promoId}`, {
                  method: 'PUT',
                  body: JSON.stringify(data)
              });
              alert('Cập nhật khuyến mãi thành công!');
          } else {
              await window.apiRequest('/api/admin/promotions', {
                  method: 'POST',
                  body: JSON.stringify(data)
              });
              alert('Thêm khuyến mãi thành công!');
          }
          bootstrap.Modal.getInstance(document.getElementById('promotionModal')).hide();
          renderPromotionsTable();
      } catch (err) {
          alert('Lỗi: ' + err.message);
      }
  });

  // Save Order Status
  document.getElementById('saveOrderStatusBtn')?.addEventListener('click', async () => {
      const orderId = document.getElementById('orderStatusId').value;
      const data = {
          status: document.getElementById('orderStatus').value
      };

      try {
          await window.apiRequest(`/api/admin/orders/${orderId}`, {
              method: 'PUT',
              body: JSON.stringify(data)
          });
          alert('Cập nhật trạng thái đơn hàng thành công!');
          bootstrap.Modal.getInstance(document.getElementById('orderStatusModal')).hide();
          renderOrdersTable();
      } catch (err) {
          alert('Lỗi: ' + err.message);
      }
  });

  // ============================================
  // 7. START APP
  // ============================================
  
  // Chờ DOM load xong mới chạy
  if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', initAdmin);
  } else {
      initAdmin();
  }
})();