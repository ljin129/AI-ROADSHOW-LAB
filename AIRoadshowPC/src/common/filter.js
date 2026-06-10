import Vue from 'vue'

Vue.filter('changeNum', (val) => {
	let num1 = 0;
	let num2 = 0;
	val.map((item) => {
			if (item.Type == 1 || item.treeNodeType== 1) {
					num1 += 1;
			} else if (item.Type == 0|| item.treeNodeType== 0) {
					num2 += 1;
			}
	});
	if (num1 == 0 && num2 == 0) {
		return "";
	} else if (num1 == 0 && num2 != 0) {
		return "当前已选择" + num2 + "部门";
	} else if (num1 != 0 && num2 == 0) {
		return "当前已选择" + num1 + "人";
	} else if (num1 != 0 && num2 != 0) { 
		return "当前已选择" + num1 + "人," + num2 + '部门';
	}
})

Vue.filter('changeName', (val) => {
	let num1 = 0;
	let num2 = 0;
	if (val && val.length > 0) { 
		val.map((item) => {
			item.DepUsers.map((v) => { 
				if (v.Type == 1) {
					num1 += 1;
				} else if (v.Type == 0) {
					num2 += 1;
				}
			})
			
		});
	}
	
	if (num1 == 0 && num2 == 0) {
		return "";
	} else if (num1 == 0 && num2 != 0) {
		return "当前已选择" + num2 + "部门";
	} else if (num1 != 0 && num2 == 0) {
		return "当前已选择" + num1 + "人";
	} else if (num1 != 0 && num2 != 0) { 
		return "当前已选择" + num1 + "人," + num2 + '部门';
	}
})