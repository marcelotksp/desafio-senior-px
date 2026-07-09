/* =====================================================================
   apiService — mirrors the Hubie ticketService.
   - builds the headers (form-urlencoded Content-Type + Bearer Authorization);
   - POSTs { method, data } to the given .ashx.

   AUTH and CATEGORIES are IMPLEMENTED as a reference. The TICKET endpoints are
   
   ===================================================================== */
angular.module('hubieTest').factory('apiService',
    ['$http', '$httpParamSerializerJQLike', '$sessionStorage',
    function ($http, $httpParamSerializerJQLike, $sessionStorage) {

        // backend base (.ashx). Change it if you host the backend on another port.
        var WEB_SERVER = $sessionStorage.webServer || '/';

        function headers() {
            var h = { 'Content-Type': 'application/x-www-form-urlencoded;' };
            if ($sessionStorage.X_User_Token) {
                h['Authorization'] = 'Bearer ' + $sessionStorage.X_User_Token;
            }
            return h;
        }

        // generic request in the Hubie style
        function request(url, method, data) {
            var params = { method: method, data: data };
            return $http({
                method: 'POST',
                url: WEB_SERVER + url,
                data: $httpParamSerializerJQLike(params),
                headers: headers()
            });
        }

        // Multipart upload (file + form fields via FormData)
        function upload(url, formData) {
            var h = {};
            if ($sessionStorage.X_User_Token) {
                h['Authorization'] = 'Bearer ' + $sessionStorage.X_User_Token;
            }
            // Let the browser set Content-Type with boundary automatically
            h['Content-Type'] = undefined;
            return $http({
                method: 'POST',
                url: WEB_SERVER + url,
                data: formData,
                headers: h,
                transformRequest: angular.identity
            });
        }

        return {
            // ---------------- AUTH (reference) ----------------
            login: function (login, password) {
                return request('ashx/auth/starter.ashx', 'authlogin',
                    JSON.stringify({ login: login, password: password }));
            },

            // ---------------- CATEGORIES (reference) ----------------
            listCategories: function () {
                return request('ashx/process/categories.ashx', 'list', null);
            },

            // ─────────────── TICKET ───────────────────────────────────────

            /** REQUESTER: open a new ticket */
            openTicket: function (ticket) {
                return request('ashx/process/ticket.ashx', 'open',
                    JSON.stringify(ticket));
            },

            /** REQUESTER: list their own tickets */
            listMyTickets: function () {
                return request('ashx/process/ticket.ashx', 'listMine', null);
            },

            /** AGENT: get the queue (optional status filter) */
            listQueue: function (status) {
                return request('ashx/process/ticket.ashx', 'listQueue',
                    JSON.stringify({ status: status || '' }));
            },

            /** BOTH: load ticket header */
            getTicket: function (ticketId) {
                return request('ashx/process/ticket.ashx', 'get',
                    JSON.stringify({ ticketId: ticketId }));
            },

            /** AGENT: assign ticket to themselves */
            assign: function (ticketId) {
                return request('ashx/process/ticket.ashx', 'assign',
                    JSON.stringify({ ticketId: ticketId }));
            },

            /** AGENT (+ REQUESTER for CLOSE): change ticket status */
            changeStatus: function (ticketId, status) {
                return request('ashx/process/ticket.ashx', 'changeStatus',
                    JSON.stringify({ ticketId: ticketId, status: status }));
            },

            /** BOTH: add a message to the thread */
            addInteraction: function (ticketId, message) {
                return request('ashx/process/ticket.ashx', 'addInteraction',
                    JSON.stringify({ ticketId: ticketId, message: message }));
            },

            /** BOTH: list the thread for a ticket */
            listInteractions: function (ticketId) {
                return request('ashx/process/ticket.ashx', 'listInteractions',
                    JSON.stringify({ ticketId: ticketId }));
            },

            /** BOTH: list attachments for a ticket */
            listAttachments: function (ticketId) {
                return request('ashx/process/ticket.ashx', 'listAttachments',
                    JSON.stringify({ ticketId: ticketId }));
            },

            // ─────────────── ATTACHMENT ───────────────────────────────────

            /** BOTH: upload a file to a ticket */
            uploadAttachment: function (ticketId, file) {
                var fd = new FormData();
                fd.append('method', 'upload');
                fd.append('ticketId', ticketId);
                fd.append('file', file);
                return upload('ashx/process/attachment.ashx', fd);
            },

            /** BOTH: build a download URL for an attachment */
            downloadUrl: function (ticketId, attachmentId) {
                return WEB_SERVER + 'ashx/process/attachment.ashx' +
                    '?method=download' +
                    '&ticketId=' + ticketId +
                    '&attachmentId=' + attachmentId +
                    '&token=' + encodeURIComponent($sessionStorage.X_User_Token || '');
            },

            // exposes the generic request so the candidate can use it freely
            request: request,
            webServer: WEB_SERVER
        };
    }
]);
