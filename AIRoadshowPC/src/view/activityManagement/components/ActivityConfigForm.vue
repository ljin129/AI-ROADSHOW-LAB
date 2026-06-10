<template>
	<div>
		<div class="section-title">活动配置</div>
		<el-form label-width="130px" class="config-form">
			<el-form-item label="活动名称" required>
				<el-input v-model.trim="form.name" maxlength="40" show-word-limit placeholder="请输入活动名称" />
			</el-form-item>
			<el-form-item label="活动描述" required>
				<el-input
					v-model.trim="form.desc"
					type="textarea"
					:rows="4"
					maxlength="200"
					show-word-limit
					placeholder="请输入活动描述"
				/>
			</el-form-item>
			<el-form-item label="封面图">
				<el-upload
					class="cover-upload"
					:action="uploadAction"
					list-type="picture-card"
					:file-list="coverUploadList"
					:auto-upload="true"
					:limit="1"
					:http-request="handleCoverUploadRequest"
					:on-remove="handleCoverRemove"
					:on-exceed="handleCoverExceed"
					:before-upload="beforeCoverUpload"
					:disabled="uploading"
					accept=".jpg,.jpeg,.png,.gif,.webp,.bmp"
				>
					<i class="el-icon-plus" />
				</el-upload>
				<div class="upload-tip">支持 JPG / PNG / GIF / WEBP / BMP，单张不超过 10MB。</div>
			</el-form-item>
			<el-form-item label="客户背景" required>
				<el-input
					v-model.trim="form.background"
					type="textarea"
					:rows="4"
					maxlength="300"
					show-word-limit
					placeholder="请输入客户背景"
				/>
			</el-form-item>
			<el-form-item label="活动时间" required>
				<el-date-picker
					v-model="form.time"
					type="datetimerange"
					range-separator="至"
					start-placeholder="开始时间"
					end-placeholder="结束时间"
					value-format="yyyy-MM-dd HH:mm:ss"
					class="full-width"
				/>
			</el-form-item>
			<el-form-item label="建议测评时长" required>
				<el-input-number v-model="form.duration" :min="5" :max="180" />
				<span class="suffix-text">分钟</span>
			</el-form-item>
			<el-form-item label="发布状态">
				<el-switch v-model="form.published" active-text="已发布" inactive-text="未发布" />
			</el-form-item>
		</el-form>
	</div>
</template>

<script>
import { getFullUrl } from '@/utils'

export default {
	name: 'ActivityConfigForm',
	props: {
		form: {
			type: Object,
			required: true,
		},
		uploading: {
			type: Boolean,
			default: false,
		},
	},
	computed: {
		uploadAction() {
			return `${process.env.VUE_APP_BASEURL}/Beta.FileServiceV2/Api/UserFile?modelID=61015&type=uploadimage&Storage=aicoach`
		},
		coverUploadList() {
			if (!this.form.coverImage) {
				return []
			}
			return [
				{
					name: 'cover-image',
					url: getFullUrl(this.form.coverImage, 320),
					status: 'success',
					uid: 'activity-cover-image',
				},
			]
		},
	},
	methods: {
		beforeCoverUpload(file) {
			const suffix = file.name.substring(file.name.lastIndexOf('.') + 1).toLowerCase()
			const allowList = ['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp']
			if (allowList.indexOf(suffix) === -1) {
				this.$message.error('仅支持 JPG、PNG、GIF、WEBP、BMP 图片')
				return false
			}
			if (file.size / 1024 / 1024 > 10) {
				this.$message.error('封面图大小不能超过 10MB')
				return false
			}
			return true
		},
		handleCoverUploadRequest(options) {
			this.$emit('upload-cover', options)
		},
		handleCoverRemove() {
			this.$emit('update-cover', '')
		},
		handleCoverExceed() {
			this.$message.warning('仅支持上传 1 张封面图，请先删除原图片再上传')
		},
	},
}
</script>

<style scoped>
.section-title {
	margin-bottom: 20px;
	font-size: 18px;
	font-weight: 700;
	color: #1f2d3d;
}

.config-form {
	max-width: 780px;
}

.full-width {
	width: 100%;
}

.suffix-text {
	margin-left: 8px;
	color: #606266;
}

.cover-upload /deep/ .el-upload--picture-card,
.cover-upload /deep/ .el-upload-list__item {
	width: 148px;
	height: 148px;
}

.upload-tip {
	margin-top: 8px;
	font-size: 12px;
	line-height: 1.5;
	color: #909399;
}
</style>
